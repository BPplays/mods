
Player = false
IsSprinting = false

local triedSprint = false
ForceSprint = true

registerForEvent('onInit', function()
	Player = Game.GetPlayer()
	dmp = Dump(Player, false)
	dmp = DumpType('PlayerPuppet', false)
	spdlog.debug(dmp)
	-- print(dmp)
	Player:RegisterInputListener(Player)

	Observe('PlayerPuppet', 'OnGameAttached', function(self)
		self:RegisterInputListener(self)
	end)

    Observe('SprintEvents', 'OnEnter', function()
        IsSprinting = true
    end)
    Observe('SprintEvents', 'OnExit', function()
        IsSprinting = false
    end)


		act = NewObject("ListenerAction")
		print(dump(act))

	Override('SprintDecisions', 'OnAction', function(self, action, consumer, wrapped)
		-- Optionally call original logic

		-- If you want to force sprint behavior:
		-- You can't set m_sprintPressed directly (private), but you could call
		-- EnableOnEnterCondition(true) or replicate the logic.

		-- Example: always enable sprint condition when a custom flag is set
		if (ForceSprint and (not IsSprinting)) then
			self:EnableOnEnterCondition(true)
			-- maybe prevent original logic continuation
			-- return true
		end

		act = NewObject("ListenerAction")
		print(dump(action))

		-- local res = wrapped(action, consumer)
		local res = wrapped("sprint", consumer)
		return res
	end)
end)




