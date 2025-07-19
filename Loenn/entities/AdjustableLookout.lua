local watchtower = {}

watchtower.name = "ExCameraDynamics/AdjustableLookout"
watchtower.depth = -8500
watchtower.justification = {0.5, 1.0}
watchtower.nodeLineRenderType = "line"
watchtower.texture = "objects/lookout/lookout05"
watchtower.nodeLimits = {0, -1}
watchtower.placements = {
    name = "default",
    alternativeName = {"lookout", "binoculars"},
    data = {
		maxZoom = 1.0,
		minZoom = 1.0,
        onlyY = false
    }
}

return watchtower