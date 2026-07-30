local M = {}

local timer = 0
local interval = 15

local function setFpsLimit()
	if scenetree.maincef then
		scenetree.maincef:setMaxFPSLimit(999)
		log("I", "cef_fps", "CEF FPS unlocked")
	end
end

function M.onUpdate(dt)
	timer = timer + dt

	if timer >= interval then
		log("I", "cef_fps", "new timer ver. loaded")
		log("I", "cef_fps", "dt: " .. dt)
		timer = 0
		setFpsLimit()
	end
end

local function onExtensionLoaded()
	setFpsLimit()
end

local function onExtensionUnloaded()
	reloadModules(extensions.unload)
end

M.onExtensionLoaded = onExtensionLoaded
M.onExtensionUnloaded = onExtensionUnloaded

return M
