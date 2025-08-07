IsSprinting = false

registerForEvent('onInit', function()
    Observe('SprintEvents', 'OnEnter', function()
        IsSprinting = true
    end)
    Observe('SprintEvents', 'OnExit', function()
        IsSprinting = false
    end)
end)

local triedSprint = false

-- onUpdate runs every game frame
registerForEvent("onUpdate", function(deltaTime)
    local player = Game.GetPlayer()                          -- :contentReference[oaicite:3]{index=3}
    if not player then return end

    -- Discover if the player is already sprinting
    local isSprinting = player.IsSprinting                   -- discovered via DumpType :contentReference[oaicite:4]{index=4}
    if type(isSprinting) == "function" then
        isSprinting = player:IsSprinting()
    end

    -- Get stamina value from the Stat Pools System
    local statSys = GetSingleton("gameStatPoolsSystem")      -- :contentReference[oaicite:5]{index=5}
    local stamina, maxStamina = statSys:GetValue(player, gamedataStatPoolType.Stamina), statSys:GetMaxValue(player, gamedataStatPoolType.Stamina)

    -- If we have enough stamina and aren’t already sprinting, try to sprint
    if stamina > 0.1 and not isSprinting then
        if not triedSprint then
            -- Native function names may vary; check NativeDB for your CET version:
            -- e.g. player:RequestSprintStart() or player:StartSprint()
            player:RequestSprintStart()
        end
        triedSprint = true
    else
        triedSprint = false
    end
end)
