#ifndef TYPE_CONVERT_H
#define TYPE_CONVERT_H

#include "cOgre.h"
#include "cNameValueParamsHandle.h"
#include "cCustomFrameListener.h"

#ifdef __cplusplus
#include <Ogre.h>

namespace invision
{
	inline Vector3 toVector3(Ogre::Vector3& v)
	{
		Vector3 r;
		r.x = v.x;
		r.y = v.y;
		r.z = v.z;

		return r;
	}

	inline Ogre::Vector3 fromVector3(Vector3& v)
	{
		return Ogre::Vector3(v.x, v.y, v.z);
	}

	inline Quaternion toQuaternion(Ogre::Quaternion& v)
	{
		Quaternion q;
		q.w = v.w;
		q.x = v.x;
		q.y = v.y;
		q.z = v.z;

		return q;
	}

	inline Ogre::Quaternion fromQuaternion(Quaternion& q)
	{
		return Ogre::Quaternion(q.w, q.x, q.y, q.z);
	}

	inline Ogre::Root* asRoot(HRoot handle)
	{
		return (Ogre::Root*)handle;
	}

	inline CustomFrameListener* asCustomFrameListener(HFrameListener handle)
	{
		return (CustomFrameListener*)handle;
	}

	inline Ogre::RenderSystem* asRenderSystem(HRenderSystem handle)
	{
		return (Ogre::RenderSystem*)handle;
	}

	inline Ogre::RenderWindow* asRenderWindow(HRenderWindow handle)
	{
		return (Ogre::RenderWindow*)handle;
	}

	inline Ogre::NameValuePairList* asNameValuePairList(HNameValuePairList handle)
	{
		return (Ogre::NameValuePairList*)handle;
	}

	inline Ogre::SceneManager* asSceneManager(HSceneManager handle)
	{
		return (Ogre::SceneManager*)handle;
	}

	inline Ogre::SceneNode* asSceneNode(HSceneNode handle)
	{
		return (Ogre::SceneNode*)handle;
	}

	inline Ogre::Camera* asCamera(HCamera handle)
	{
		return (Ogre::Camera*)handle;
	}

	inline Ogre::Node* asNode(HNode handle)
	{
		return (Ogre::Node*)handle;
	}

	inline Ogre::TextureManager* asTextureManager(HTextureManager handle)
	{
		return (Ogre::TextureManager*)handle;
	}

	inline Ogre::Viewport* asViewport(HViewport handle)
	{
		return (Ogre::Viewport*)handle;
	}

	inline Ogre::ConfigFile* asConfigFile(HConfigFile handle)
	{
		return (Ogre::ConfigFile*)handle;
	}

	inline Ogre::ResourceGroupManager* asResGroupManager(HResourceGroupManager handle)
	{
		return (Ogre::ResourceGroupManager*)handle;
	}

	inline Ogre::MaterialManager* asMaterialManager(HMaterialManager handle)
	{
		return (Ogre::MaterialManager*)handle;
	}
}

#endif // __cplusplus

#endif // TYPE_CONVERT_H
