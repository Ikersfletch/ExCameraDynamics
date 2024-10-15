local atlases = require("atlases")
local utils = require("utils")
local mods = require("mods")

local zoomParallax = {}
zoomParallax.name = "ExCameraDynamics/ZoomParticleDepth"

local zoomParallaxTexturePrefix = "bgs/"
local zoomParallaxTexturePath = "Graphics/Atlases/Gameplay/bgs"

local fieldOrder = {
    "textures", "only", "exclude", "tag",
    "flag", "notflag", "blendmode", "color",
    "x", "y",
	
	"particleCount",
	
	"particleSpreadX", "particleSpreadY",
	
	"minParticleDepth", "maxParticleDepth",
	
	"minParticleSpeedX", "maxParticleSpeedX",
	"minParticleSpeedY", "maxParticleSpeedY",
	
	"baseParticleAngle", "particleAngleSpread",
	"minParticleAngleSpeed", "maxParticleAngleSpeed",
	
	"minParticleWindAffect", "maxParticleWindAffect",
	
    "fadex", "fadey", "fadez",
    "alpha"
}

local defaultData = {
    x = 0.0,
    y = 0.0,

    alpha = 1.0,
    color = "FFFFFF",

	instantIn = false,
	instantOut = false,

    only = "*",
    exclude = "",

    textures = "",

    loopx = true,
    loopy = true,

    flag = "",
    notflag = "",

    blendmode = "alphablend",
    fadeIn = false,

    fadex = "",
    fadey = "",
    fadez = "",

    tag = "",
	
	particleCount = 100,
	
	particleSpreadX = 320, particleSpreadY = 180,
	
	minParticleDepth = 0.0,
	maxParticleDepth = 1.0,
	
	minParticleSpeedX = 0.0,
	maxParticleSpeedX = 0.0,
	
	minParticleSpeedY = 0.0,
	maxParticleSpeedY = 0.0,
	
	baseParticleAngle = 0.0,
	particleAngleSpread = 0.0,
	minParticleAngleSpeed = 0.0,
	maxParticleAngleSpeed = 0.0,
	minParticleWindAffect = 0.0,
	maxParticleWindAffect = 0.0,
	
	particleScale = 1.0,
}

zoomParallax.miscAtlasLookup = table.flip({
    "darkswamp", "fmod", "fna", "mist",
    "monogame", "northernlights", "purplesunset",
    "vignette", "whiteCube", "xna",
})

local fieldInformation = {
	particleCount = {
		fieldType = "integer"
	},
	particleSpreadX = {
		fieldType = "integer"
	},
	particleSpreadY = {
		fieldType = "integer"
	},
    color = {
        fieldType = "color"
    },
    blendmode = {
        options = {
            "additive",
            "alphablend"
        },
        editable = false
    },
    textures = {
        fieldType = "path",
        filePickerExtensions = {"png"},
        allowMissingPath = false,
        celesteAtlas = "Gameplay",
        earlyValidator = function(filename)
            if zoomParallax.miscAtlasLookup[filename] then
                return true
            end
        end,
        filenameProcessor = function(filename)
            -- Discard leading "Graphics/Atlases/Gui/" and file extension
            local filename, ext = utils.splitExtension(filename)
            local parts = utils.splitpath(filename, "/")

            return utils.convertToUnixPath(utils.joinpath(unpack(parts, 4)))
        end,
        filenameResolver = function(filename, text, prefix)
            return string.format("%s/Graphics/Atlases/Gameplay/%s.png", prefix, text)
        end
    },
}

function zoomParallax.getParallaxNames()
    local res = {}
    local added = {}

    -- Any loaded sprites
    for name, sprite in pairs(atlases.gameplay) do
        if utils.startsWith(name, zoomParallaxTexturePrefix) then
            added[name] = true
            added[sprite.meta.filename] = true

            table.insert(res, name)
        end
    end

    -- Mod content sprites
    -- Some of these might have already been loaded
    local filenames = mods.findModFiletype(zoomParallaxTexturePath, "png")
    local zoomParallaxPathLength = #zoomParallaxTexturePath

    for i, name in ipairs(filenames) do
        if not added[name] then
            local nameNoExt, ext = utils.splitExtension(name)
            if ext == "png" then
                -- Remove mod specific path, keep bgs/ prefix
                local firstSlashIndex = utils.findCharacter(nameNoExt, "/")
                local resourceName = nameNoExt:sub(firstSlashIndex + zoomParallaxPathLength - 2)

                if not added[resourceName] then
                    table.insert(res, resourceName)
                end
            end

            if yield and i % 100 == 0 then
                coroutine.yield()
            end
        end
    end

    return res
end

function zoomParallax.defaultData(style)
    return defaultData
end

function zoomParallax.fieldOrder(style)
    return fieldOrder
end

function zoomParallax.fieldInformation(style)
    return fieldInformation
end

function zoomParallax.canForeground(style)
    return true
end

function zoomParallax.canBackground(style)
    return true
end

function zoomParallax.languageData(language, style)
    return language.style.parallax
end

-- TODO - Language file
function zoomParallax.displayName(language, style)
    local texture = style.textures

    return string.format("Zoom Particle (Depth) - %s", texture)
end

function zoomParallax.associatedMods(style)
    local texture = style.textures
    local sprite = atlases.gameplay[texture]

    if sprite then
        -- Skip internal files, they don't belong to a mod
        if sprite.internalFile then
            return
        end

        local filename = sprite.filename
        local modMetadata = mods.getModMetadataFromPath(filename)

        return mods.getModNamesFromMetadata(modMetadata)
    end
end

return zoomParallax