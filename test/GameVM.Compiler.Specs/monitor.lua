-- GameVM MAME Monitor Script
-- Runs the emulated program until it reaches the halt loop (JMP *),
-- then dumps CPU state to stdout for verification in tests.
-- If the program never halts, MAME is killed by the test runner's timeout
-- (a hung test is a failure).

local function dump_state()
    local cpu = manager.machine.devices[":maincpu"]

    print("--- GAMEVM MAME DUMP ---")
    print("GAMEVM PROGRAM HALTED")
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

-- True when the CPU is sitting on the halt loop: a JMP absolute (opcode 0x4C)
-- whose target address is the address of the JMP itself.
local function is_halted()
    local ok, result = pcall(function()
        local cpu = manager.machine.devices[":maincpu"]
        local pc = cpu.state["PC"].value
        local mem = cpu.spaces["program"]
        if mem:read_u8(pc) ~= 0x4C then
            return false
        end
        local target = mem:read_u8(pc + 1) + mem:read_u8(pc + 2) * 256
        return target == pc
    end)
    return ok and result
end

local function on_frame_callback(mach)
    if is_halted() then
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

-- Pattern 5: emu.wait-based polling loop (fallback).
-- Polls until the halt loop is seen; the runner's process timeout is the
-- backstop if the program never halts.
if not registered then
    local function run_loop()
        while true do
            if emu and emu.wait then
                emu.wait(1.0/60.0)
            elseif os and os.execute then
                os.execute("sleep 0.016")
            else
                break
            end
            if is_halted() then
                dump_state()
                if manager and manager.machine then
                    manager.machine:exit()
                end
                break
            end
        end
    end
    run_loop()
end
