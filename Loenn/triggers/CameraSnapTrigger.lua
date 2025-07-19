local ZoomTrigger = {}

ZoomTrigger.name = "ExCameraDynamics/CameraSnapTrigger"
ZoomTrigger.category = "camera"
ZoomTrigger.fieldOrder = {
    "x", "y", "width", "height", "snapSpeed"
}
ZoomTrigger.fieldInformation = {
	snapSpeed = {
		fieldType = "number"
	},
}
ZoomTrigger.placements = {
    name = "default",
    data = {
		snapSpeed = 1,
    }
}
return ZoomTrigger