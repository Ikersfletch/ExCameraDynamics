local watchtower = {}

watchtower.name = "ExCameraDynamics/ReferenceFrameLookout"
watchtower.depth = -8500
watchtower.justification = {0.5, 1.0}
watchtower.texture = "ExCameraDynamics/lookout"
watchtower.placements = {
    name = "default",
    alternativeName = {"lookout", "binoculars"},
    data = {
        onlyY = false,
		easyKeys = ""
    }
}

return watchtower