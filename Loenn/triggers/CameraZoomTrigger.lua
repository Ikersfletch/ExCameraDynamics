local ZoomTrigger = {}

ZoomTrigger.name = "ExCameraDynamics/CameraZoomTrigger"
ZoomTrigger.category = "camera"
ZoomTrigger.fieldOrder = {
    "x", "y", "width", "height", "positionMode","isMax", "zoomStart", "zoomEnd"
}
ZoomTrigger.fieldInformation = {
    deleteFlag = {
		fieldType = "string",
		editable = true
	},
	zoomEnd = {
		fieldType = "number"
	},
	zoomStart = {
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
	isMax = {
		fieldType = "boolean"
	}
}
ZoomTrigger.placements = {
    name = "default",
    data = {
		zoomEnd = 1,
		zoomStart = 1,
		positionMode = "NoEffect",
		isMax = true,
		deleteFlag = ""
    }
}
return ZoomTrigger