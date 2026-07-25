-- GameVM MAME Monitor Script
-- Dumps CPU state to stdout for verification in tests

local function dump_state()
    local cpu = manager.machine.devices[":maincpu"]
    
    print("--- GAMEVM MAME DUMP ---")
    print("CPU state:")
    print("A: " .. string.format("%02X", cpu.state["A"].value))
    print("X: " .. string.format("%02X", cpu.state["X"].value))
    print("Y: " .. string.format("%02X", cpu.state["Y"].value))
    print("PC: " .. string.format("%04X", cpu.state["PC"].value))

    print("TIA/RAM Dump:")
    local mem = cpu.spaces["program"]
    for i = 0x80, 0x85 do
        local val = 0
        pcall(function() val = mem:read_u8(i) end)
        if val == 0 then pcall(function() val = mem:read_byte(i) end) end
        print(string.format("$%02X: %02X", i, val))
    end
    print("--- END GAMEVM DUMP ---")
end

local frames = 0

local function on_frame_callback(mach)
    frames = frames + 1
    if frames == 30 then
        dump_state()
        mach:exit()
    end
end

-- Try different MAME Lua API patterns for frame callbacks
local registered = false

-- Pattern 1: manager.machine.add_notifier with string "frame"
if manager and manager.machine and manager.machine.add_notifier then
    local ok = pcall(function() manager.machine:add_notifier("frame", on_frame_callback) end)
    if ok then registered = true end
end

-- Pattern 2: manager.machine.add_notifier with machine_notifier.on_frame
if not registered and manager and manager.machine and manager.machine.add_notifier and machine_notifier and machine_notifier.on_frame then
    local ok = pcall(function() manager.machine:add_notifier(machine_notifier.on_frame, on_frame_callback) end)
    if ok then registered = true end
end

-- Pattern 3: emu.frame_done (newer MAME)
if not registered and emu and emu.frame_done then
    local ok = pcall(function() emu.frame_done(on_frame_callback) end)
    if ok then registered = true end
end

-- Pattern 4: manager.machine:register_frame_done
if not registered and manager and manager.machine and manager.machine.register_frame_done then
    local ok = pcall(function() manager.machine:register_frame_done(on_frame_callback) end)
    if ok then registered = true end
end

-- Pattern 5: emu.wait-based loop (fallback)
if not registered then
    local function run_loop()
        for i = 1, 30 do
            -- Wait for next frame
            if emu and emu.wait then
                emu.wait(1.0/60.0)
            elseif os and os.execute then
                os.execute("sleep 0.016")
            end
            on_frame_callback(manager.machine)
        end
    end
    run_loop()
end