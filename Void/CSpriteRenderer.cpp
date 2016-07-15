#include "../Header/CSpriteRenderer.h"

namespace Void
{
	namespace Renderer
	{
		CSpriteRenderer::CSpriteRenderer()
			:	m_Device(NULL),
				m_ResourceManager(NULL),
				m_VertexDeclaration(NULL),
				m_VertexBuffer(NULL),
				m_IndexBuffer(NULL),
				m_ScreenWidth(0.0f),
				m_ScreenHeight(0.0f),
				m_ActiveQueueBackground(0),
				m_ActiveQueueForeground(0)
		{
			m_BackgroundQueueCnt[0]	= 0;
			m_BackgroundQueueCnt[1]	= 0;
			m_ForegroundQueueCnt[0]	= 0;
			m_ForegroundQueueCnt[1]	= 0;
		}

		CSpriteRenderer::~CSpriteRenderer()
		{
			this->Release();
		}

		void CSpriteRenderer::Release()
		{
			SAFE_RELEASE(m_VertexBuffer);
			SAFE_RELEASE(m_IndexBuffer);
			SAFE_RELEASE(m_VertexDeclaration);
			m_JobQueueBackground[0].clear();
			m_JobQueueBackground[1].clear();
			m_JobQueueForeground[0].clear();
			m_JobQueueForeground[1].clear();
			m_Device				= NULL;
			m_ScreenWidth			= 0.0f;
			m_ScreenHeight			= 0.0f;
			m_BackgroundQueueCnt[0]	= 0;
			m_BackgroundQueueCnt[1]	= 0;
			m_ForegroundQueueCnt[0]	= 0;
			m_ForegroundQueueCnt[1]	= 0;
		}

		bool CSpriteRenderer::Initialize(IDirect3DDevice9* const device, const float32 width, const float32 height)
		{
			m_Device = device;
			m_ScreenWidth = width;
			m_ScreenHeight = height;

			const D3DVERTEXELEMENT9 decl[3] = 
			{
			  {0, 0,  D3DDECLTYPE_FLOAT3, D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_POSITION, 0},
			  {0, 4*3, D3DDECLTYPE_FLOAT2, D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_TEXCOORD, 0},
			  D3DDECL_END()
			};
			HRESULT hr = m_Device->CreateVertexDeclaration(decl, &m_VertexDeclaration);
			if(FAILED(hr))
			{
				DEBUG_MSG("CreateVertexDeclaration Failed. [CSpriteRenderer::Initialize]");
				return false;
			}

			hr = m_Device->CreateVertexBuffer(	MAX_NUM_SPRITES * sizeof(Vertex_Sprite) * 4,
												D3DUSAGE_DYNAMIC | D3DUSAGE_WRITEONLY,
												NULL,
												D3DPOOL_DEFAULT,
												&m_VertexBuffer,
												NULL);
			if(FAILED(hr))
			{
				DEBUG_MSG("CreateVertexBuffer Failed. [CSpriteRenderer::Initialize]");
				return false;
			}
			
			hr = m_Device->CreateIndexBuffer(	MAX_NUM_SPRITES * sizeof(uint16) * 6,
												D3DUSAGE_WRITEONLY,
												D3DFMT_INDEX16,
												D3DPOOL_MANAGED,
												&m_IndexBuffer,
												NULL);
			if(FAILED(hr))
			{
				DEBUG_MSG("CreateIndexBuffer Failed. [CSpriteRenderer::Initialize]");
				return false;
			}

			uint16* pIndices;
			hr = m_IndexBuffer->Lock(0, 0, (void**)&pIndices, NULL);
			if(FAILED(hr))
			{
				DEBUG_MSG("Lock IndexBuffer Failed. [CSpriteRenderer::Initialize]");
				return false;
			}

			uint32 j = 0;
			uint32 k = 0;
			for(uint32 i = 0; i < MAX_NUM_SPRITES; ++i)
			{
				//1st tri
				pIndices[j+0] = k+0;
				pIndices[j+1] = k+1;
				pIndices[j+2] = k+3;

				//2nd tri
				pIndices[j+3] = k+3;
				pIndices[j+4] = k+1;
				pIndices[j+5] = k+2;

				j += 6;
				k += 4;
			}
			hr = m_IndexBuffer->Unlock();
			if(FAILED(hr))
			{
				DEBUG_MSG("Unlock IndexBuffer Failed. [CSpriteRenderer::Initialize]");
				return false;
			}

			m_SpriteProjMatrix.OrthoLH(m_ScreenWidth, m_ScreenHeight, -10000.0f, 10000.0f);
			m_SpriteViewMatrix.LookAtLH(CVector3(m_ScreenWidth/2, m_ScreenHeight/2, -1.0f),
										CVector3(m_ScreenWidth/2, m_ScreenHeight/2, 0.0f),
										CVector3(0.0f, 1.0f, 0.0f));
			m_SpriteWorldMatrix.Identity();

			return true;
		}

		void CSpriteRenderer::AddRenderJob(RenderJob_Sprite* const job)
		{
			if(job->SpriteCnt == 0)
				return;

			if(!job->IsCurtain)
			{
				if((m_BackgroundQueueCnt[m_ActiveQueueBackground] + job->SpriteCnt) < MAX_NUM_SPRITES)
				{
					job->RebuildSortingKey();
					m_BackgroundQueueCnt[m_ActiveQueueBackground] += job->SpriteCnt;
					m_JobQueueBackground[m_ActiveQueueBackground].push_back(job);
				}
				else
				{
					DEBUG_MSG("Exceeded MAX_NUM_SPRITES. [CSpriteRenderer::AddRenderJob]");
				}
			}
			else
			{
				if((m_ForegroundQueueCnt[m_ActiveQueueForeground] + job->SpriteCnt) < MAX_NUM_SPRITES)
				{
					job->RebuildSortingKey();
					m_ForegroundQueueCnt[m_ActiveQueueForeground] += job->SpriteCnt;
					m_JobQueueForeground[m_ActiveQueueForeground].push_back(job);
				}
				else
				{
					DEBUG_MSG("Exceeded MAX_NUM_SPRITES. [CSpriteRenderer::AddRenderJob]");
				}
			}
		}

		void CSpriteRenderer::Render(std::deque<RenderJob_Sprite* const>& jobQueue)
		{
			if(jobQueue.empty())
				return;

			std::sort(jobQueue.begin(), jobQueue.end(), RenderJob_Sprite::Compare);

			//prepare vertexbuffer
			Vertex_Sprite* vertices;
			HRESULT hr = m_VertexBuffer->Lock(0, 0, (void**)&vertices, D3DLOCK_DISCARD);
			if(FAILED(hr))
			{
				DEBUG_MSG("Lock VertexBuffer Failed. [CSpriteRenderer::Render]");
			}

			//enter sprites into vertexbuffer
			uint16 currentVertexPos = 0;
			uint16 size = jobQueue.size();
			for(uint16 i = 0; i < size; ++i)
			{
				this->EnterSpriteIntoBuffer(&vertices[currentVertexPos], jobQueue[i]);
				currentVertexPos += 4 * jobQueue[i]->SpriteCnt;
			}			

			hr = m_VertexBuffer->Unlock();
			if(FAILED(hr))
			{
				DEBUG_MSG("Unlock VertexBuffer Failed. [CSpriteRenderer::Render]");
			}
			
			//do render
			hr = m_Device->SetStreamSource(0, m_VertexBuffer, 0, sizeof(Vertex_Sprite));
			if(FAILED(hr))
			{
				DEBUG_MSG("SetStreamSource Failed. [CSpriteRenderer::Render]");
			}

			hr = m_Device->SetVertexDeclaration(m_VertexDeclaration);
			if(FAILED(hr))
			{
				DEBUG_MSG("SetVertexDeclaration Failed. [CSpriteRenderer::Render]");
			}

			hr = m_Device->SetIndices(m_IndexBuffer);
			if(FAILED(hr))
			{
				DEBUG_MSG("SetIndices Failed. [CSpriteRenderer::Render]");
			}
			
			TextureId currentTexId = TextureId_Default;
			EffectId currentFxId = EffectId_Default;
			float32 currentFAlpha = -1.0f;

			CEffect* effect			= NULL;
 			uint16 minVertexIndex	= 0;
 			uint16 startIndex		= 0;
			
			for(uint16 i = 0; i < size; ++i)
			{
				bool newFx = (jobQueue[i]->EffectId != currentFxId);
				if(newFx)
				{
					currentFxId = jobQueue[i]->EffectId;
					effect = m_ResourceManager->GetEffectById(currentFxId);
					effect->SetMatrix("matProj", &m_SpriteProjMatrix);
					effect->SetMatrix("matView", &m_SpriteViewMatrix);
				}
				
				bool newTex = (jobQueue[i]->TextureId != currentTexId);
				if(newTex)
					currentTexId = jobQueue[i]->TextureId;

				if(newFx || newTex)
					effect->SetTexture("diffuseTexture", m_ResourceManager->GetTextureById(currentTexId));

				if(jobQueue[i]->FinalAlpha != currentFAlpha || newFx)
				{
					currentFAlpha = jobQueue[i]->FinalAlpha;
					effect->SetFloat("finalAlpha", currentFAlpha);
				}

				uint16 numPasses = effect->BeginRender();
				for(uint16 pass = 0; pass < numPasses; pass++)
				{
					effect->BeginPass(pass);
					
					hr = m_Device->DrawIndexedPrimitive(	D3DPT_TRIANGLELIST, 
															0, 
															minVertexIndex, 
															jobQueue[i]->SpriteCnt * 4, 
															startIndex, 
															jobQueue[i]->SpriteCnt * 2);
					if(FAILED(hr))
					{
						DEBUG_MSG("DrawIndexedPrimitive Failed. [CSpriteRenderer::Render]");
					}

					effect->EndPass();
				}
				effect->EndRender();

				minVertexIndex += jobQueue[i]->SpriteCnt * 4;
				startIndex += jobQueue[i]->SpriteCnt * 6;
			}
			
			jobQueue.clear();
		}

		void CSpriteRenderer::EnterSpriteIntoBuffer(Vertex_Sprite* const vertices, const RenderJob_Sprite* const job)
		{			
			uint16 index = 0;
			float32 minX, minY, maxX, maxY = 0.0f;
			RenderJob_Sprite::Sprite sprite;

			uint16 size = job->SpriteCnt;
			for(uint16 i = 0; i < size; ++i)
			{
				sprite = job->SpritePtr[i];
				vertices[0+index].Texture0_U		= sprite.TexCoordMin.X;
				vertices[0+index].Texture0_V		= 1.0f - sprite.TexCoordMax.Y;

				vertices[1+index].Texture0_U		= sprite.TexCoordMin.X;
				vertices[1+index].Texture0_V		= 1.0f - sprite.TexCoordMin.Y;

				vertices[2+index].Texture0_U		= sprite.TexCoordMax.X;
				vertices[2+index].Texture0_V		= 1.0f - sprite.TexCoordMin.Y;

				vertices[3+index].Texture0_U		= sprite.TexCoordMax.X;
				vertices[3+index].Texture0_V		= 1.0f - sprite.TexCoordMax.Y;

				minX = sprite.PositionMin.X;
				minY = m_ScreenHeight - sprite.PositionMin.Y;
				maxX = sprite.PositionMax.X;
				maxY = m_ScreenHeight - sprite.PositionMax.Y;
				
				vertices[0+index].Position	= CVector3(minX, minY, 0.0f);
				vertices[1+index].Position	= CVector3(minX, maxY, 0.0f);
				vertices[2+index].Position	= CVector3(maxX, maxY, 0.0f);
				vertices[3+index].Position	= CVector3(maxX, minY, 0.0f);

				index += 4;
			}
		}

		void CSpriteRenderer::RenderQuad(CTexture* const quadTexture)
		{
			//prepare vertexbuffer
			Vertex_Sprite* vertices;
			HRESULT hr = m_VertexBuffer->Lock(0, 4, (void**)&vertices, D3DLOCK_DISCARD);
			if(FAILED(hr))
			{
				DEBUG_MSG("Lock VertexBuffer Failed. [CSpriteRenderer::RenderQuad]");
			}

			
			RenderJob_Sprite::Sprite sprite = RenderJob_Sprite::Sprite();
			sprite.PositionMin = CVector2(0.0f, 0.0f);
			sprite.PositionMax = CVector2(m_ScreenWidth, m_ScreenHeight);
			
			vertices[0].Texture0_U		= sprite.TexCoordMin.X;
			vertices[0].Texture0_V		= 1.0f - sprite.TexCoordMax.Y;

			vertices[1].Texture0_U		= sprite.TexCoordMin.X;
			vertices[1].Texture0_V		= 1.0f - sprite.TexCoordMin.Y;

			vertices[2].Texture0_U		= sprite.TexCoordMax.X;
			vertices[2].Texture0_V		= 1.0f - sprite.TexCoordMin.Y;

			vertices[3].Texture0_U		= sprite.TexCoordMax.X;
			vertices[3].Texture0_V		= 1.0f - sprite.TexCoordMax.Y;

			float32 minX = sprite.PositionMin.X - 1;
			float32 minY = m_ScreenHeight - sprite.PositionMin.Y + 1;
			float32 maxX = sprite.PositionMax.X;
			float32 maxY = m_ScreenHeight - sprite.PositionMax.Y;
			
			vertices[0].Position	= CVector3(minX, minY, 0.0f);
			vertices[1].Position	= CVector3(minX, maxY, 0.0f);
			vertices[2].Position	= CVector3(maxX, maxY, 0.0f);
			vertices[3].Position	= CVector3(maxX, minY, 0.0f);		

			hr = m_VertexBuffer->Unlock();
			if(FAILED(hr))
			{
				DEBUG_MSG("Unlock VertexBuffer Failed. [CSpriteRenderer::RenderQuad]");
			}

			//do render
			hr = m_Device->SetStreamSource(0, m_VertexBuffer, 0, sizeof(Vertex_Sprite));
			if(FAILED(hr))
			{
				DEBUG_MSG("SetStreamSource Failed. [CSpriteRenderer::RenderQuad]");
			}

			hr = m_Device->SetVertexDeclaration(m_VertexDeclaration);
			if(FAILED(hr))
			{
				DEBUG_MSG("SetVertexDeclaration Failed. [CSpriteRenderer::RenderQuad]");
			}

			hr = m_Device->SetIndices(m_IndexBuffer);
			if(FAILED(hr))
			{
				DEBUG_MSG("SetIndices Failed. [CSpriteRenderer::RenderQuad]");
			}
			
			CEffect* effect = m_ResourceManager->GetPostProcessingEffect();
			effect->SetMatrix("matProj", &m_SpriteProjMatrix);
			effect->SetMatrix("matView", &m_SpriteViewMatrix);
			effect->SetTexture("diffuseTexture", quadTexture);
			
			uint16 numPasses = effect->BeginRender();
			for(uint16 pass = 0; pass < numPasses; pass++)
			{
				effect->BeginPass(pass);
				
				hr = m_Device->DrawIndexedPrimitive(	D3DPT_TRIANGLELIST, 
														0, 
														0, 
														4, 
														0, 
														2);
				if(FAILED(hr))
				{
					DEBUG_MSG("DrawIndexedPrimitive Failed. [CSpriteRenderer::RenderQuad]");
				}

				effect->EndPass();
			}
			effect->EndRender();
		}
		
		void CSpriteRenderer::RenderDeferredQuad(CTexture* const diffuseTexture, CTexture* const depthTexture, CTexture* const normalTexture)
		{
			//prepare vertexbuffer
			Vertex_Sprite* vertices;
			HRESULT hr = m_VertexBuffer->Lock(0, 4, (void**)&vertices, D3DLOCK_DISCARD);
			if(FAILED(hr))
			{
				DEBUG_MSG("Lock VertexBuffer Failed. [CSpriteRenderer::RenderQuad]");
			}

			
			RenderJob_Sprite::Sprite sprite = RenderJob_Sprite::Sprite();
			sprite.PositionMin = CVector2(0.0f, 0.0f);
			sprite.PositionMax = CVector2(m_ScreenWidth, m_ScreenHeight);
			
			vertices[0].Texture0_U		= sprite.TexCoordMin.X;
			vertices[0].Texture0_V		= 1.0f - sprite.TexCoordMax.Y;

			vertices[1].Texture0_U		= sprite.TexCoordMin.X;
			vertices[1].Texture0_V		= 1.0f - sprite.TexCoordMin.Y;

			vertices[2].Texture0_U		= sprite.TexCoordMax.X;
			vertices[2].Texture0_V		= 1.0f - sprite.TexCoordMin.Y;

			vertices[3].Texture0_U		= sprite.TexCoordMax.X;
			vertices[3].Texture0_V		= 1.0f - sprite.TexCoordMax.Y;

			float32 minX = sprite.PositionMin.X - 1;
			float32 minY = m_ScreenHeight - sprite.PositionMin.Y + 1;
			float32 maxX = sprite.PositionMax.X;
			float32 maxY = m_ScreenHeight - sprite.PositionMax.Y;
			
			vertices[0].Position	= CVector3(minX, minY, 0.0f);
			vertices[1].Position	= CVector3(minX, maxY, 0.0f);
			vertices[2].Position	= CVector3(maxX, maxY, 0.0f);
			vertices[3].Position	= CVector3(maxX, minY, 0.0f);		

			hr = m_VertexBuffer->Unlock();
			if(FAILED(hr))
			{
				DEBUG_MSG("Unlock VertexBuffer Failed. [CSpriteRenderer::RenderQuad]");
			}

			//do render
			hr = m_Device->SetStreamSource(0, m_VertexBuffer, 0, sizeof(Vertex_Sprite));
			if(FAILED(hr))
			{
				DEBUG_MSG("SetStreamSource Failed. [CSpriteRenderer::RenderQuad]");
			}

			hr = m_Device->SetVertexDeclaration(m_VertexDeclaration);
			if(FAILED(hr))
			{
				DEBUG_MSG("SetVertexDeclaration Failed. [CSpriteRenderer::RenderQuad]");
			}

			hr = m_Device->SetIndices(m_IndexBuffer);
			if(FAILED(hr))
			{
				DEBUG_MSG("SetIndices Failed. [CSpriteRenderer::RenderQuad]");
			}
			
			CEffect* effect = m_ResourceManager->GetPostProcessingEffect();
			effect->SetMatrix("matProj", &m_SpriteProjMatrix);
			effect->SetMatrix("matView", &m_SpriteViewMatrix);
			effect->SetTexture("diffuseTexture", diffuseTexture);
			effect->SetTexture("depthTexture", depthTexture);
			effect->SetTexture("normalTexture", normalTexture);
			
			uint16 numPasses = effect->BeginRender();
			for(uint16 pass = 0; pass < numPasses; pass++)
			{
				effect->BeginPass(pass);
				
				hr = m_Device->DrawIndexedPrimitive(	D3DPT_TRIANGLELIST, 
														0, 
														0, 
														4, 
														0, 
														2);
				if(FAILED(hr))
				{
					DEBUG_MSG("DrawIndexedPrimitive Failed. [CSpriteRenderer::RenderQuad]");
				}

				effect->EndPass();
			}
			effect->EndRender();
		}
	};
};