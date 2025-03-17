local ZoomTrigger = {}

ZoomTrigger.name = "ExCameraDynamics/CameraSliderZoomTrigger"
ZoomTrigger.fieldOrder = {
    "x", "y", "width", "height", "mode","isMax", "zoomStart", "zoomEnd"
}
ZoomTrigger.fieldInformation = {
    deleteFlag = {
		fieldType = "string",
		editable = true
	},
	zoomEnd = {
		fieldType = "string"
	},
	zoomStart = {
		fieldType = "string"
	},
	mode = {
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
	isMax = {
		fieldType = "boolean"
	}
}
ZoomTrigger.placements = {
    name = "default",
    data = {
		zoomEnd = "1",
		zoomStart = "1",
		mode = "Start",
		isMax = true,
		deleteFlag = ""
    }
}
return ZoomTrigger