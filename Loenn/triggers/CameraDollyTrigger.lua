local ZoomTrigger = {}

ZoomTrigger.name = "ExCameraDynamics/CameraDollyTrigger"
ZoomTrigger.category = "camera"
ZoomTrigger.fieldOrder = {
    "x", "y", "width", "height", "zoomStart", "zoomEnd", "duration","isMax"
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
	isMax = {
		fieldType = "boolean"
	},
	duration = {
		fieldType = "number"
	}
}
ZoomTrigger.placements = {
    name = "default",
    data = {
		zoomEnd = "1",
		zoomStart = "1",
		duration = 1,
		isMax = true,
		deleteFlag = ""
    }
}
return ZoomTrigger