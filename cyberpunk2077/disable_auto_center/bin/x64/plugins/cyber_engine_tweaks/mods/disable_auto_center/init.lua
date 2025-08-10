
local enableLogging = false

local mod_name = "disable_auto_center"
local mod_name_pretty = "Disable Auto Center"

local settings = {
	disable_for_mouse = true,
	disable_for_gamepad = true,
}

local default_values = {
	mouse = 5,
	mouse2w = 5000,
	gamepad = false,
	gamepad2w = false,
}

function SaveSettings()
	local ok, content = pcall(function() return json.encode(settings) end)

	if ok and content ~= nil then
		local file = io.open("settings.json", "w+")
		if file == nil then return false end
		file:write(content)
		file:close()
	end
	return true
end


function LoadSettings()
	local file = io.open('settings.json', 'r')
	if file ~= nil then
		local contents = file:read("*a")
		local ok, savedSettings = pcall(function() return json.decode(contents) end)
		file:close()

		if ok then
			for key, _ in pairs(settings) do
				if savedSettings[key] ~= nil then
					settings[key] = savedSettings[key]
				end
			end
		end
	end
end

function setFlats()
	local mouse_value = {
		def = -1.0,
		def2w = -1.0,
	}
	if settings.disable_for_mouse then
		mouse_value.def = -1.0
		mouse_value.def2w = -1.0
	else
		mouse_value.def = default_values.mouse
		mouse_value.def2w = default_values.mouse2w
	end

	print(mouse_value.def2w)

	TweakDB:SetFlat(
		"Camera.VehicleTPP_DefaultParams.AutoCenterStartTimeMouse",
		mouse_value.def
	)
	TweakDB:SetFlat(
		"Camera.VehicleTPP_2w_DefaultParams.AutoCenterStartTimeMouse",
		mouse_value.def2w
	)




	local gamepad_value = {
		def = -1.0,
		def2w = -1.0,
	}

	if settings.disable_for_mouse then
		gamepad_value.def = -1.0
		gamepad_value.def2w = -1.0
	else
		gamepad_value.def = default_values.mouse
		gamepad_value.def2w = default_values.mouse2w
	end
	if settings.disable_for_gamepad then
		TweakDB:SetFlat(
			"Camera.VehicleTPP_DefaultParams.AutoCenterStartTimeGamepad",
			gamepad_value.def
		)
		TweakDB:SetFlat(
			"Camera.VehicleTPP_2w_DefaultParams.AutoCenterStartTimeGamepad",
			gamepad_value.def2w
		)
	end
end

registerForEvent('onInit', function()


	default_values.mouse = TweakDB:GetFlat("Camera.VehicleTPP_DefaultParams.AutoCenterStartTimeMouse")

	default_values.mouse2w = TweakDB:GetFlat("Camera.VehicleTPP_2w_DefaultParams.AutoCenterStartTimeMouse")
	print(default_values.mouse2w)

	default_values.gamepad = TweakDB:GetFlat("Camera.VehicleTPP_DefaultParams.AutoCenterStartTimeGamepad")

	default_values.gamepad2w = TweakDB:GetFlat("Camera.VehicleTPP_2w_DefaultParams.AutoCenterStartTimeGamepad")


	local file = io.open('settings.json', 'r')
	if file == nil then
		SaveSettings()
	else
		file:close()
	end


	LoadSettings()


	NativeSettings = GetMod("nativeSettings")
	NativeSettings.addTab(("/%s"):format(mod_name), mod_name_pretty)

	NativeSettings.addSwitch(("/%s"):format(mod_name), "Disable Auto Center for Mouse", "Description", true, true, function(state)
		settings.disable_for_mouse = state
		SaveSettings()
	end)

	NativeSettings.addSwitch(("/%s"):format(mod_name), "Disable Auto Center for Controller", "Description", true, true, function(state)
		settings.disable_for_mouse = state
		SaveSettings()
	end)

	setFlats()

end)




