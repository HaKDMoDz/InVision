#include "cOIS.h"
#include "cCustomMouseListener.h"

/**
 * Method: MouseListener::CustomMouseListener
 */
INV_EXPORT InvHandle
INV_CALL new_custommouselistener(MouseMovedHandler mouseMoved, MouseClickHandler mousePressed, MouseClickHandler mouseReleased)
{
	CustomMouseListener* mouseListener = new CustomMouseListener(mouseMoved, mousePressed, mouseReleased);

	return createHandle(mouseListener);
}

/**
 * Method: MouseListener::~MouseListener (OK)
 */
INV_EXPORT void
INV_CALL delete_mouselistener(InvHandle self)
{
	destroyHandle(self);
}
