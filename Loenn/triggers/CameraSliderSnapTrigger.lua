local ZoomTrigger = {}

ZoomTrigger.name = "ExCameraDynamics/CameraSliderSnapTrigger"
ZoomTrigger.fieldOrder = {
    "x", "y", "width", "height", "snapSpeed"
}
ZoomTrigger.fieldInformation = {
	snapSpeed = {
		fieldType = "string"
	},
}
ZoomTrigger.placements = {
    name = "default",
    data = {
		snapSpeed = 1,
    }
}
return ZoomTrigger