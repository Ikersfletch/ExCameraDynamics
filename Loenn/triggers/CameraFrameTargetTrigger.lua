local ZoomTrigger = {}

ZoomTrigger.name = "ExCameraDynamics/CameraFrameTargetTrigger"
ZoomTrigger.category = "camera"
ZoomTrigger.fieldOrder = {
    "x", "y", "width", "height", "easyKey", "lerpStrength", "positionMode","xOnly","yOnly", "zoomStart"
}
ZoomTrigger.fieldInformation = {
    deleteFlag = {
		fieldType = "string",
		editable = true
	},
    easyKey = {
		fieldType = "string",
		editable = true
	},
	zoomStart = {
		fieldType = "number"
	},
	lerpStrength = {
		fieldType = "number"
	},
	positionMode = {
		fieldType = "string",
		editable = false,
		options = {
			NoEffect="NoEffect",
			TopToBottom="TopToBottom",
			BottomToTop="BottomToTop",
			LeftToRight="LeftToRight",
			RightToLeft="RightToLeft",
			HorizontalCenter="HorizontalCenter",
			VerticalCenter="VerticalCenter"
		}
		
	},
	xOnly = {
		fieldType = "boolean"
	},
	yOnly = {
		fieldType = "boolean"
	}
}
ZoomTrigger.placements = {
    name = "default",
    data = {
		zoomStart = 1,
		lerpStrength = 1,
		positionMode = "NoEffect",
		easyKey = "",
		xOnly = false,
		yOnly = false,
		deleteFlag = ""
    }
}
return ZoomTrigger