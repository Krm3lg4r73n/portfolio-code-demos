#include "../Header/CEntity3D.h"

namespace Void
{
	namespace Scene
	{
		CEntity3D::CEntity3D(const CHashedString& id) 
			: Identifier(id), BoundingBox(new CBoundingBox())
		{
			this->WorldMatrix.Identity();
			this->BoundingBox->SetMatrix(&this->WorldMatrix);
		}
	
		CEntity3D::~CEntity3D()
		{
			SAFE_DELETE(this->BoundingBox);

			ConstantMap::iterator iter = this->m_ConstantMap.begin();
			while(iter != this->m_ConstantMap.end())
			{
				SAFE_DELETE(iter->second);
				iter++;
			}
			this->m_ConstantMap.clear();
		}
	
		void CEntity3D::Update(const float32 timeDelta)
		{
			LuaObject tmpObj = Lua->GetGlobalActorTable()[this->Identifier.GetString().c_str()];
			if(tmpObj.IsTable())
			{
				LuaObject funcObj = tmpObj["OnUpdate"];
				if(funcObj.IsFunction())
				{
					LuaFunction<void> func = funcObj;
					try
					{
						func(timeDelta);
					}
					catch(LuaException ex)
					{
						DEBUG_MSG_VA("[CEntity3D::Update]", 
							"Failed to execute ActorFuntion of: %s\nLuaFunction OnUpdate has caused the exception\n'%s'", 
							this->Identifier.GetString().c_str(), ex.GetErrorMessage());
					}
				}
			}
		}
	
		bool CEntity3D::SetShaderConstant(ShaderConstant* constant)
		{
			ConstantMap::iterator iter = this->m_ConstantMap.find(constant->Identifier);
			if(iter == this->m_ConstantMap.end())
			{
				ConstantMapEnt entry = ConstantMapEnt(constant->Identifier, constant);
				ConstantMapIRes iRes = this->m_ConstantMap.insert(entry);
				return iRes.second;
			}
			else
			{
				SAFE_DELETE(iter->second)
				iter->second = constant;
			}
			return true;
		}
	
		void CEntity3D::SetShaderConstants(CEffect* const effect, const uint16 pass)
		{
			ConstantMap::iterator iter = this->m_ConstantMap.begin();
			while(iter != this->m_ConstantMap.end())
			{
				//skip this constant if its not the right pass
				if(iter->second->Pass != pass)
				{
					iter++;
					continue;
				}

				switch(iter->second->Type)
				{
				case ShaderConstant::CONSTANT_TYPE_BOOL:
					effect->SetBool(iter->second->Identifier.GetString().c_str(), *((bool*)iter->second->Value));
					break;
				case ShaderConstant::CONSTANT_TYPE_INT:
					effect->SetInt(iter->second->Identifier.GetString().c_str(), *((int32*)iter->second->Value));
					break;
				case ShaderConstant::CONSTANT_TYPE_FLOAT:
					effect->SetFloat(iter->second->Identifier.GetString().c_str(), *((float32*)iter->second->Value));
					break;
				case ShaderConstant::CONSTANT_TYPE_VECTOR3:
					effect->SetVector(iter->second->Identifier.GetString().c_str(), (CVector3*)iter->second->Value);
					break;
				case ShaderConstant::CONSTANT_TYPE_VECTOR4:
					effect->SetVector(iter->second->Identifier.GetString().c_str(), (CVector4*)iter->second->Value);
					break;
				case ShaderConstant::CONSTANT_TYPE_VECTOR4_ARRAY:
					effect->SetVectorArray(iter->second->Identifier.GetString().c_str(), (CVector4*)iter->second->Value, iter->second->ArraySize);
					break;
				case ShaderConstant::CONSTANT_TYPE_MATRIX4X4:
					effect->SetMatrix(iter->second->Identifier.GetString().c_str(), (CMatrix4x4*)iter->second->Value);
					break;
				case ShaderConstant::CONSTANT_TYPE_MATRIX4X4_ARRAY:
					effect->SetMatrixArray(iter->second->Identifier.GetString().c_str(), (CMatrix4x4*)iter->second->Value, iter->second->ArraySize);
					break;
				default:
					DEBUG_MSG("ShaderConstantType unknown. [CEntity3D::SetShaderConstants]");
					break;
				}
				iter++;
			}
		}		
	};
};