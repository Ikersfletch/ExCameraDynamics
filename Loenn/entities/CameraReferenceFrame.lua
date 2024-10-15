local utils = require("utils")
local drawableSprite = require("structs.drawable_sprite")
local drawableRectangle = require("structs.drawable_rectangle")
local drawableText = require("structs.drawable_text")

local frame = {}
frame.name = "ExCameraDynamics/CameraReferenceFrame"
frame.depth = -20000

frame.fieldOrder = {
    "x", "y", "easyKey", "zoom"
}
frame.fieldInformation = {
    easyKey = {
        fieldType = "string",
		editable = true
    },
	zoom = {
		fieldType = "number",
		minimumValue = 0.125
	}
}
frame.placements = {
    {
        name = "default",
		data = {
			zoom = 1.0,
			easyKey = ""
		}
    }
}



function frame.sprite(room, entity)
	local xoffset = 160.0 / entity.zoom
	local yoffset = 90.0 / entity.zoom

	local sprites = {}

	local topLeft = drawableSprite.fromTexture(
		"ExCameraDynamics/zoomLimit", 
		{
			x = entity.x - xoffset,
			y = entity.y - yoffset 
		}
	)
	local bottomRight = drawableSprite.fromTexture(
		"ExCameraDynamics/zoomLimit", 
		{
			x = entity.x + xoffset,
			y = entity.y + yoffset
		}
	)
	
	topLeft:setJustification(0.0,0.0)
	bottomRight:setJustification(0.0,0.0)
	bottomRight:setScale(-1,-1)
	
	local rectangle = drawableRectangle.fromRectangle(
		"line",
		entity.x - xoffset,
		entity.y - yoffset,
		xoffset * 2,
		yoffset * 2,
		{ 1.0, 1.0, 1.0, 0.5 }
	)
	
	table.insert(sprites, rectangle)
	table.insert(sprites, topLeft)
	table.insert(sprites, bottomRight)
	
	if entity.easyKey then
		local bgrect = drawableRectangle.fromRectangle(
			"fill",
			entity.x - xoffset,
			entity.y - yoffset - 8,
			string.len(entity.easyKey) * 4 + 3,
			8,
			{ 0.0, 0.0, 0.0, 0.8 }
		)
	
		local text = drawableText.fromText(
			entity.easyKey,
			entity.x - xoffset + 2,
			entity.y - yoffset - 6
		)
		
		table.insert(sprites, bgrect)
		table.insert(sprites, text)
	end
	
	return sprites
end




function frame.selection(room, entity)
    return utils.rectangle(entity.x - (160.0 / entity.zoom), entity.y - (90.0 / entity.zoom), 320.0 / entity.zoom, 180.0 / entity.zoom)
end

return frame
