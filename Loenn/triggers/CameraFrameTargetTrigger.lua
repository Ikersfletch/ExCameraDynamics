local ZoomTrigger = {}

ZoomTrigger.name = "ExCameraDynamics/CameraFrameTargetTrigger"
ZoomTrigger.category = "camera"
ZoomTrigger.fieldOrder = {
    "x", "y", "width", "height", "easyKey", "lerpStrength", "lerpMode","xOnly","yOnly", "zoomStart"
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
	lerpMode = {
		fieldType = "string",
		editable = false,
		options = {
			Start="Start",
			TopToBottom="TopToBottom",
			BottomToTop="BottomToTop",
			LeftToRight="LeftToRight",
			RightToLeft="RightToLeft"
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
		lerpMode = "Start",
		easyKey = "",
		xOnly = false,
		yOnly = false,
		deleteFlag = ""
    }
}
return ZoomTrigger