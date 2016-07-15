/*
	Base 3D scene entity class (abstract).
*/

#ifndef _CENITITY3D_H_
#define _CENITITY3D_H_

#include <map>
#include "../../Core/Header/Void.h"
#include "../../Core/Header/CHashedString.h"
#include "../../Core/Header/CLua.h"
#include "../../Renderer/Header/CRenderer.h"
#include "../../ResourceManagement/Header/CResourceManager.h"
#include "../../ResourceManagement/Header/IShaderConstantSetter.h"
#include "../../Math/Header/CBoundingBox.h"
#include "../../Math/Header/CMatrix4x4.h"
#include "SceneTypes.h"

using namespace Void::Core;
using namespace Void::Renderer;
using namespace Void::ResourceManagement;
using namespace Void::Math;
using namespace LuaPlus;

namespace Void
{
	namespace Scene
	{
		enum Entity3DType
		{ 
			ENTITY3D_MODEL = 0x0,
			ENTITY3D_PARTICLE_SYSTEM,
			ENTITY3D_BILLBOARD
		};
	
		typedef std::map<const CHashedString, ShaderConstant*>	ConstantMap;
		typedef std::pair<const CHashedString, ShaderConstant*>	ConstantMapEnt;
		typedef std::pair<ConstantMap::iterator, bool>		ConstantMapIRes;

		class CEntity3D : public IShaderConstantSetter
		{
		private:
			ConstantMap				m_ConstantMap;

		public:
			CHashedString			Identifier;

			//global world transformation (determined by scene graph)
			CMatrix4x4				WorldMatrix;

			//global object oriented bounding-box
			CBoundingBox*			BoundingBox;

		public:
			explicit CEntity3D(const CHashedString& id);
			virtual ~CEntity3D();
			
			virtual void Update(const float32 timeDelta);
			virtual void PreRender(CRenderer* const renderer) = 0;

			virtual void LoadResources(CResourceManager* const resManager) = 0;
			virtual void UnloadResources(CResourceManager* const resManager) = 0;

			virtual void Rebuild() = 0;

			bool SetShaderConstant(ShaderConstant* constant);

			virtual Entity3DType GetEntityType() const = 0;

			//from IShaderConstantSetter
			void SetShaderConstants(CEffect* const effect, const uint16 pass);
		};
	};
};

#endif
