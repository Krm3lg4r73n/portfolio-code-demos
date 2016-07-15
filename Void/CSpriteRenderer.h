/*
	Renders screen aligned quads used for GUI or other 
	2D visualization.
*/

#ifndef _CSPRITERENDERER_H_
#define _CSPRITERENDERER_H_

#include <d3d9.h>
#include <deque>
#include <algorithm>
#include <vector>
#include "../../Core/Header/Void.h"
#include "../../Core/Header/CTimer.h"
#include "../../Core/Header/CLog.h"
#include "../../ResourceManagement/Header/CResourceManager.h"
#include "../../Math/Header/CMatrix4x4.h"
#include "RendererTypes.h"

using namespace Void::Core;
using namespace Void::ResourceManagement;
using namespace Void::Math;

namespace Void
{
	namespace Renderer
	{
		#define MAX_NUM_SPRITES		10000

		struct Vertex_Sprite
		{
			CVector3	Position;
			float32		Texture0_U;
			float32		Texture0_V;
		};

		class CSpriteRenderer
		{
		private:
			IDirect3DDevice9*							m_Device;
			CResourceManager*							m_ResourceManager;

			std::deque<RenderJob_Sprite* const>			m_JobQueueBackground[2];
			uint16										m_BackgroundQueueCnt[2];
			uint8										m_ActiveQueueBackground;

			std::deque<RenderJob_Sprite* const>			m_JobQueueForeground[2];
			uint16										m_ForegroundQueueCnt[2];
			uint8										m_ActiveQueueForeground;

			IDirect3DVertexDeclaration9*				m_VertexDeclaration;
			IDirect3DVertexBuffer9*						m_VertexBuffer;
			IDirect3DIndexBuffer9*						m_IndexBuffer;

			CMatrix4x4									m_SpriteViewMatrix;
			CMatrix4x4									m_SpriteProjMatrix;
			CMatrix4x4									m_SpriteWorldMatrix;
			float32										m_ScreenHeight;
			float32										m_ScreenWidth;

		private:
			void EnterSpriteIntoBuffer (Vertex_Sprite* const vertices, const RenderJob_Sprite* const job);
			void Render(std::deque<RenderJob_Sprite* const>& jobQueue);

		public:
			CSpriteRenderer();
			~CSpriteRenderer();

			bool Initialize(IDirect3DDevice9* const device, const float32 width, const float32 height);
			void Release();

			void AddRenderJob(RenderJob_Sprite* const job);

			//used for post processing only
			void RenderQuad(CTexture* const quadTexture);
			
			//testing deferred render
			void RenderDeferredQuad(CTexture* const diffuseTexture, CTexture* const depthTexture, CTexture* const normalTexture);

			inline void RenderBackground()
			{
				//swap queues
				uint8 oldQueue = m_ActiveQueueBackground;
				m_ActiveQueueBackground = (m_ActiveQueueBackground + 1) % 2;

				//use old queue for render
				this->Render(m_JobQueueBackground[oldQueue]);
				m_BackgroundQueueCnt[oldQueue] = 0;
			}

			inline void RenderForeground()
			{
				//swap queues
				uint8 oldQueue = m_ActiveQueueForeground;
				m_ActiveQueueForeground = (m_ActiveQueueForeground + 1) % 2;

				//use old queue for render
				this->Render(m_JobQueueForeground[oldQueue]);
				m_ForegroundQueueCnt[oldQueue] = 0;
			}
			
			inline void InjectResourceManager(CResourceManager* const resManager)
			{
				m_ResourceManager = resManager;
			}
		};
	};
};

#endif