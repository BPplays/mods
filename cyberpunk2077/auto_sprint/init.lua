
local enableLogging = false
local enableLoggingSact = false

SprintObj = false
SprintSet = false


NoSprintObj = false
NoSprintSet = false

Player = false
IsSprinting = false
WantSprint = true

local SprintPressed = false
ForceSprint = true
local ignoreActions = {
	['BUTTON_RELEASED'] = {
		['UI_FakeMovement'] = true,
	},
	['RELATIVE_CHANGE'] = {
		['UI_FakeCamera'] = true,
		['CameraMouseX'] = true,
		['CameraMouseY'] = true,
		['mouse_x'] = true,
		['mouse_y'] = true,
	},
}

local spoof = {
  GetName = function() return "Sprint" end,
  GetValue = function() return 1.0 end
}



registerInput('bp.auto_sprint.toggle', 'toggle sprint', function(keypress)
    if not keypress then
        return
    end
	WantSprint = not WantSprint

	if enableLogging then
		print("toggle WantSprint", WantSprint)
	end

end)


registerForEvent('onInit', function()
	Player = Game.GetPlayer()
	-- dmp = Dump(Player, false)
	-- dmp = DumpType('PlayerPuppet', false)
	-- spdlog.info(dmp)
	-- print(dmp)
	Player:RegisterInputListener(Player)


	Observe('PlayerPuppet', 'OnGameAttached', function(self)
		self:RegisterInputListener(self)
	end)

	Observe("PlayerPuppet", "OnAction", function(_, action)
		-- print(Game.NameToString(action:GetName()))
		if not action then return end

		-- spdlog.info(Dump(action))
		-- print(Dump(action))

		if action:GetType().value == "BUTTON_PRESSED" then
			if Game.NameToString(action:GetName()) == "Sprint" then
				-- Sprint button is pressed (or active)
				-- print("Sprinting!")
				if action:GetValue() > 0 then
					SprintObj = action
					SprintSet = true

					if not SprintPressed then
						WantSprint = not WantSprint
						if enableLogging then
							print("toggle WantSprint", WantSprint)
						end
						SprintPressed = true
					end


					if enableLogging then
						spdlog.info('sprint')
					end
				end

			end
		elseif action:GetType().value == "BUTTON_HOLD_COMPLETE" then
			if Game.NameToString(action:GetName()) == "Sprint" then
				-- Sprint button is pressed (or active)
				-- print("Sprinting!")
				if action:GetValue() <= 0 or true then
					if enableLogging then
						spdlog.info('no sprint')
					end
					NoSprintObj = action
					NoSprintSet = true
					SprintPressed = false
				else
				end

			end

		elseif action:GetType().value == "BUTTON_RELEASED" then
			if Game.NameToString(action:GetName()) == "Sprint" then
				SprintPressed = false
			end
		end

		if enableLogging then
			local actionName = Game.NameToString(action:GetName())
			local actionType = action:GetType().value -- gameinputActionType
			local actionValue = action:GetValue()

			if not ignoreActions[actionType] or not ignoreActions[actionType][actionName] then
				spdlog.info(('read[%s] %s = %.3f'):format(actionType, actionName, actionValue))
			end
		end
	end)



    Observe('SprintEvents', 'OnEnter', function()
        IsSprinting = true
    end)
    Observe('SprintEvents', 'OnExit', function()
        IsSprinting = false
    end)



	Override('SprintDecisions', 'OnAction', function(self, action, consumer, wrapped)
		local res = wrapped(action, consumer)
		self.sprintPressed = WantSprint
		return res
	end)

	Override('SprintDecisions', 'EnterCondition', function(self, stateContext, scriptInterface, wrap)
		self.sprintPressed = WantSprint
		return wrap(stateContext, scriptInterface)
	end)


end)




