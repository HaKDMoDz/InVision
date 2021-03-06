#ifndef __INVISIONNATIVE_OIS_AXIS_DESCRIPTOR_H__
#define __INVISIONNATIVE_OIS_AXIS_DESCRIPTOR_H__

#include <InvisionHandle.h>
#include "invisionnative_ois_component_descriptor.h"
#include "invisionnative_ois.h"

extern "C"
{
	/**
	 * Type AxisDescriptor
	 */
	struct AxisDescriptor
	{
		ComponentDescriptor base;
		_int* abs;
		_int* rel;
		_bool* absOnly;
	};
	
	AxisDescriptor descriptor_of_axis(InvHandle handle);
	
}

#endif // __INVISIONNATIVE_OIS_AXIS_DESCRIPTOR_H__

