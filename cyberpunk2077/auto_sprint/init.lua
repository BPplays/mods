
local enableLogging = true

SprintObj = false
SprintSet = false


NoSprintObj = false
NoSprintSet = false

Player = false
IsSprinting = false
WantSprint = true

local triedSprint = false
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





registerInput('bp.auto_sprint.toggle', 'toggle sprint', function(keypress)

	if keypress then
		WantSprint = false
		-- key is pressed
	else
		WantSprint = false
		-- key is released
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

		if action:GetType().value == "BUTTON_PRESSED" or true then
			if Game.NameToString(action:GetName()) == "Sprint" then
				-- Sprint button is pressed (or active)
				-- print("Sprinting!")
				if action:GetValue() > 0 then
					SprintObj = action
					SprintSet = true
					if enableLogging then
						spdlog.info('sprint')
					end
				end

			end
		elseif action:GetType().value == "BUTTON_RELEASED" then
			if Game.NameToString(action:GetName()) == "Sprint" then
				-- Sprint button is pressed (or active)
				-- print("Sprinting!")
				if action:GetValue() <= 0 then
					if enableLogging then
						spdlog.info('no sprint')
					end
					NoSprintObj = action
					NoSprintSet = true
				else
				end

			end
		end

		if enableLogging then
			local actionName = Game.NameToString(action:GetName())
			local actionType = action:GetType().value -- gameinputActionType
			local actionValue = action:GetValue()

			if not ignoreActions[actionType] or not ignoreActions[actionType][actionName] then
				spdlog.info(('[%s] %s = %.3f'):format(actionType, actionName, actionValue))
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

		if (ForceSprint and (not IsSprinting)) then
			self:EnableOnEnterCondition(true)
			-- return true
		end

		-- act = NewObject("ListenerAction")
		-- act.Name = "sprint"


		if SprintSet and WantSprint then
			action = SprintObj
		elseif NoSprintSet and not WantSprint then
		end

		-- spdlog.info(Dump(action))
		-- print(Dump(action))

		-- local res = wrapped(action, consumer)
		local res = wrapped(action, consumer)
		return res
	end)
end)




