
local enableLogging = true

Player = false
WantSprint = true

local SprintPressed = false
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

local sprintActions = {
	['BUTTON_PRESSED'] = {
		['Sprint'] = true,
		['ToggleSprint'] = true,
	},
}


local noSprintActions = {
	['BUTTON_HOLD_COMPLETE'] = {
		['Sprint'] = true,
		['ToggleSprint'] = true,
	},

	['BUTTON_RELEASED'] = {
		['Sprint'] = true,
		['ToggleSprint'] = true,
	},
}


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

		local actionName = Game.NameToString(action:GetName())
		local actionType = action:GetType().value -- gameinputActionType
		local actionValue = action:GetValue()

		-- if action:GetType().value == "BUTTON_PRESSED" then
		if sprintActions[actionType] and
		sprintActions[actionType][actionName] then
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

		if noSprintActions[actionType] and
		noSprintActions[actionType][actionName] then

				if enableLogging then
					spdlog.info('nosprint')
				end
			SprintPressed = false
		end

		if enableLogging then

			if not ignoreActions[actionType] or not ignoreActions[actionType][actionName] then
				spdlog.info(('read[%s] %s = %.3f'):format(actionType, actionName, actionValue))
				spdlog.info(('is in sprint [%s]'):format(sprintActions[actionType][actionName]))
			end
		end
	end)



    -- Observe('SprintEvents', 'OnEnter', function()
    --     IsSprinting = true
    -- end)
    -- Observe('SprintEvents', 'OnExit', function()
    --     IsSprinting = false
    -- end)



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




