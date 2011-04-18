#include "LogManager.h"

using namespace invision;

__export HLogManager __entry ogre_logmanager_get_singleton()
{
	return Ogre::LogManager::getSingletonPtr();
}

__export void __entry ogre_logmanager_log_message(
		HLogManager self,
		ConstString message,
		_int messageLevel,
		_bool maskDebug)
{
	asLogManager(self)->logMessage(message, (Ogre::LogMessageLevel)messageLevel, fromBool(maskDebug));
}
