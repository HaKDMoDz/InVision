#-------------------------------------------------
#
# Project created by QtCreator 2011-02-04T18:52:09
#
#-------------------------------------------------

QT       -= core gui

TARGET = InVisionWrap_Ogre3D
TEMPLATE = lib

DEFINES += INVISIONPLATFORM_LIBRARY

SOURCES += \
	src/invision/Collections.cpp \
	src/invision/NameValueParamsHandle.cpp \
	src/invision/Util.cpp \
	src/invision/rendering/Root.cpp \
	src/invision/rendering/RenderingEnumerators.cpp \
	src/invision/rendering/RenderSystem.cpp \
	src/invision/rendering/CustomFrameListener.cpp \
    src/invision/rendering/SceneManager.cpp \
    src/invision/rendering/FrameListener.cpp \
    src/invision/Enumerator.cpp \
    src/invision/rendering/Camera.cpp \
    src/invision/Math.cpp \
    src/invision/rendering/RenderWindow.cpp \
    src/invision/Common.cpp \
    src/invision/rendering/AnimableObject.cpp


HEADERS += \
	src/invision/Collections.h \
	src/invision/NameValueParamsHandle.h \
	src/invision/Common.h \
	src/invision/Util.h \
	src/invision/rendering/CustomFrameListener.h \
	src/invision/rendering/RenderSystem.h \
	src/invision/rendering/RenderingEnumerators.h \
	src/invision/rendering/Root.h \
    src/invision/rendering/SceneManager.h \
    src/invision/rendering/FrameListener.h \
    src/invision/Enumerator.h \
    src/invision/rendering/Camera.h \
    src/invision/Math.h \
    src/invision/rendering/RenderWindow.h \
    src/invision/Ogre3D.h \
    src/invision/rendering/AnimableObject.h


Release:DESTDIR = Bin/Release
Release:OBJECTS_DIR = Bin/Release/.obj
Release:MOC_DIR = Bin/Release/.moc
Release:RCC_DIR = Bin/Release/.rcc
Release:UI_DIR = Bin/Release/.ui

Debug:DESTDIR = Bin/Debug
Debug:OBJECTS_DIR = Bin/Debug/.obj
Debug:MOC_DIR = Bin/Debug/.moc
Debug:RCC_DIR = Bin/Debug/.rcc
Debug:UI_DIR = Bin/Debug/.ui
Debug:DEFINES += DEBUG


win32 {
	DEFINES += WIN32 DEBUG

	INCLUDEPATH +=  \
		src \
		$$(OGRE_SDK)include\OGRE \
		$$(OGRE_SDK)include\OIS \
		$$(BOOST_SDK)

	LIBS += \
		-L$$(OGRE_SDK)lib\debug \
		-L$$(OGRE_SDK)lib\debug\opt \
		-L$$(BOOST_SDK)lib \
		-lOgreMain_d -lOIS_d
}