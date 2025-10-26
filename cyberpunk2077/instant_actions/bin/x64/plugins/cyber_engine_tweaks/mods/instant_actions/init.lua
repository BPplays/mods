
local enableLogging = false

Player = false
WantSprint = true
UseToggleSprint = false

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

-- local noTogSprintActions = {
-- 	['BUTTON_HOLD_COMPLETE'] = {
-- 		['ToggleSprint'] = true,
-- 	},
-- 	['BUTTON_RELEASED'] = {
-- 		['ToggleSprint'] = true,
-- 	},
-- }
--

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

			UseToggleSprint = false
			if actionName == "ToggleSprint" then
				SprintPressed = false
				if enableLogging then
					print("using tog")
				end
				UseToggleSprint = true
			end
			if enableLogging then
				print("UseToggleSprint, ", UseToggleSprint)
			end

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
		elseif noSprintActions[actionType] and
		noSprintActions[actionType][actionName] then

			-- if actionName == "ToggleSprint" then
			-- 	print("tog sprint rel got in")
			-- end
				if enableLogging then
					spdlog.info('nosprint')
				end
			SprintPressed = false

		-- elseif noTogSprintActions[actionType] and
		-- noTogSprintActions[actionType][actionName] then
		--
		-- 	if enableLogging then
		-- 		spdlog.info('disable toggle sprint')
		-- 	end
		-- 	WantSprint = false
		end

		if enableLogging then

			if not ignoreActions[actionType] or not ignoreActions[actionType][actionName] then
				spdlog.info(('read[%s] %s = %.3f'):format(actionType, actionName, actionValue))
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
		-- stateContext:SetConditionBoolParameter(CName("SprintToggled"), WantSprint, true)
		local res = wrapped(action, consumer)
		if enableLogging then
			print(Game.NameToString(action:GetName()), action:GetType().value)
		end


		self.sprintPressed = WantSprint
		self.toggleSprintPressed = false
		-- stateContext:SetConditionBoolParameter(CName("SprintToggled"), WantSprint, true)
		return res
	end)

	Override('SprintDecisions', 'EnterCondition', function(self, stateContext, scriptInterface, wrap)
		self.sprintPressed = WantSprint
		self.toggleSprintPressed = false
		-- stateContext:SetConditionBoolParameter(CName("SprintToggled"), WantSprint, true)

		-- if enableLogging then
		-- 	-- print(Dump(stateContext))
		-- 	-- print("st, ", stateContext:GetConditionBool(CName("SprintToggled")))
		-- end

		local res = wrap(stateContext, scriptInterface)

		-- stateContext:SetConditionBoolParameter(CName("SprintToggled"), WantSprint, true)
		return res
	end)


end)




