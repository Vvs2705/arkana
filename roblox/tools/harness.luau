--!nocheck
-- ============================================================================
-- ARKANA — STUBS MÍNIMOS DA API DO ROBLOX (raia 3.6: Engenheiro de QA)
-- FORA de src/: o Rojo só empacota src/shared, src/server e src/client
-- (roblox/default.project.json). Nada daqui vai para dentro do jogo.
--
-- NÃO é um motor. É o mínimo para os módulos do projeto CARREGAREM e RODAREM
-- fora do Studio, com relógio VIRTUAL (determinismo) e coleta de erros.
-- ponytail: TETO CONHECIDO — sem física real, sem replicação, sem rotação de
-- CFrame (só posição/olhar). Se um dia um teste precisar de colisão ou de
-- rotação de verdade, o upgrade é rodar no próprio Studio em modo headless.
--
-- Recebe a tabela de fontes (montada por run.sh) como vararg.
-- ============================================================================

local SOURCES = ...
local H = { errors = {}, parts = {}, humanoids = {}, lastFire = {}, fireCount = {} }

local BASE = getfenv(1) -- stdlib real (math, string, table, pairs, ...)

-- ------------------------------------------------------------ relógio virtual
-- Começa em 12s (e não em 0): `os.clock()` no Roblox é uptime do processo, e
-- há código que usa `== 0` como sentinela de "nunca começou". Zerar o relógio
-- criaria bug de laboratório que não existe no Studio.
local NOW = 12.0
function H.now(): number
	return NOW
end

-- --------------------------------------------------------------- tipos (typeof)
local TYPES = setmetatable({}, { __mode = "k" }) -- metatable -> nome do tipo

local function tagged(mt, name)
	TYPES[mt] = name
	return mt
end

local function fakeTypeof(v: any): string
	local t = type(v)
	if t ~= "table" then
		return t
	end
	local mt = getmetatable(v)
	local name = mt and TYPES[mt]
	if name then
		return name
	end
	return "table"
end

-- ------------------------------------------------------------------- Vector3
local V3 = {}
tagged(V3, "Vector3")

local function v3(x, y, z)
	return setmetatable({ X = x or 0, Y = y or 0, Z = z or 0 }, V3)
end

V3.__index = function(self, k)
	if k == "Magnitude" then
		return math.sqrt(self.X * self.X + self.Y * self.Y + self.Z * self.Z)
	elseif k == "Unit" then
		local m = math.sqrt(self.X * self.X + self.Y * self.Y + self.Z * self.Z)
		if m == 0 then
			return v3(0, 0, 0)
		end
		return v3(self.X / m, self.Y / m, self.Z / m)
	end
	return rawget(V3, k)
end
V3.__add = function(a, b)
	return v3(a.X + b.X, a.Y + b.Y, a.Z + b.Z)
end
V3.__sub = function(a, b)
	return v3(a.X - b.X, a.Y - b.Y, a.Z - b.Z)
end
V3.__mul = function(a, b)
	if type(a) == "number" then
		return v3(a * b.X, a * b.Y, a * b.Z)
	elseif type(b) == "number" then
		return v3(a.X * b, a.Y * b, a.Z * b)
	end
	return v3(a.X * b.X, a.Y * b.Y, a.Z * b.Z)
end
V3.__div = function(a, b)
	if type(b) == "number" then
		return v3(a.X / b, a.Y / b, a.Z / b)
	end
	return v3(a.X / b.X, a.Y / b.Y, a.Z / b.Z)
end
V3.__unm = function(a)
	return v3(-a.X, -a.Y, -a.Z)
end
V3.__eq = function(a, b)
	return a.X == b.X and a.Y == b.Y and a.Z == b.Z
end
V3.__tostring = function(a)
	return string.format("%.2f, %.2f, %.2f", a.X, a.Y, a.Z)
end
function V3.Dot(a, b)
	return a.X * b.X + a.Y * b.Y + a.Z * b.Z
end
function V3.Cross(a, b)
	return v3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X)
end
function V3.Lerp(a, b, t)
	return v3(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t)
end
function V3.FuzzyEq(a, b, e)
	return (a - b).Magnitude <= (e or 1e-5)
end

local Vector3 = {
	new = v3,
	zero = v3(0, 0, 0),
	one = v3(1, 1, 1),
	xAxis = v3(1, 0, 0),
	yAxis = v3(0, 1, 0),
	zAxis = v3(0, 0, 1),
}

-- ------------------------------------------------------------------- Vector2
local V2 = {}
tagged(V2, "Vector2")
local function v2(x, y)
	return setmetatable({ X = x or 0, Y = y or 0 }, V2)
end
V2.__index = function(self, k)
	if k == "Magnitude" then
		return math.sqrt(self.X * self.X + self.Y * self.Y)
	elseif k == "Unit" then
		local m = math.sqrt(self.X * self.X + self.Y * self.Y)
		return if m == 0 then v2(0, 0) else v2(self.X / m, self.Y / m)
	end
	return rawget(V2, k)
end
V2.__add = function(a, b)
	return v2(a.X + b.X, a.Y + b.Y)
end
V2.__sub = function(a, b)
	return v2(a.X - b.X, a.Y - b.Y)
end
V2.__mul = function(a, b)
	if type(a) == "number" then
		return v2(a * b.X, a * b.Y)
	elseif type(b) == "number" then
		return v2(a.X * b, a.Y * b)
	end
	return v2(a.X * b.X, a.Y * b.Y)
end
V2.__eq = function(a, b)
	return a.X == b.X and a.Y == b.Y
end
local Vector2 = { new = v2, zero = v2(0, 0), one = v2(1, 1) }

-- -------------------------------------------------------------------- Color3
local C3 = {}
tagged(C3, "Color3")
local function c3(r, g, b)
	return setmetatable({ R = r or 0, G = g or 0, B = b or 0 }, C3)
end
C3.__index = C3
C3.__eq = function(a, b)
	return a.R == b.R and a.G == b.G and a.B == b.B
end
C3.__tostring = function(a)
	return string.format("%.3f, %.3f, %.3f", a.R, a.G, a.B)
end
function C3.Lerp(a, b, t)
	return c3(a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t)
end
function C3.ToHSV(a)
	return 0, 0, math.max(a.R, a.G, a.B)
end
local Color3 = {
	new = c3,
	fromRGB = function(r, g, b)
		return c3((r or 0) / 255, (g or 0) / 255, (b or 0) / 255)
	end,
	fromHex = function(hex)
		local s = tostring(hex):gsub("#", "")
		if #s == 3 then
			s = s:sub(1, 1):rep(2) .. s:sub(2, 2):rep(2) .. s:sub(3, 3):rep(2)
		end
		local n = tonumber(s, 16) or 0
		return c3(math.floor(n / 65536) % 256 / 255, math.floor(n / 256) % 256 / 255, n % 256 / 255)
	end,
	fromHSV = function(_, _, v)
		return c3(v, v, v)
	end,
}

-- -------------------------------------------------------------------- CFrame
-- ponytail: TETO CONHECIDO — só POSIÇÃO e VETOR DE OLHAR. Rotação real (Angles,
-- composição de orientação) é ignorada; nada do servidor lê matriz de rotação.
local CF = {}
tagged(CF, "CFrame")
local function cf(pos, look)
	return setmetatable({ Position = pos or v3(0, 0, 0), LookVector = look or v3(0, 0, -1) }, CF)
end
CF.__index = function(self, k)
	if k == "X" then
		return rawget(self, "Position").X
	elseif k == "Y" then
		return rawget(self, "Position").Y
	elseif k == "Z" then
		return rawget(self, "Position").Z
	elseif k == "RightVector" then
		return v3(1, 0, 0)
	elseif k == "UpVector" then
		return v3(0, 1, 0)
	elseif k == "Rotation" then
		return cf(v3(0, 0, 0), rawget(self, "LookVector"))
	end
	return rawget(CF, k)
end
CF.__mul = function(a, b)
	if fakeTypeof(b) == "Vector3" then
		return a.Position + b
	end
	return cf(a.Position + b.Position, a.LookVector)
end
CF.__add = function(a, b)
	return cf(a.Position + b, a.LookVector)
end
CF.__sub = function(a, b)
	return cf(a.Position - b, a.LookVector)
end
CF.__tostring = function(a)
	return "CFrame(" .. tostring(a.Position) .. ")"
end
function CF.ToWorldSpace(a, b)
	return cf(a.Position + b.Position, a.LookVector)
end
function CF.ToObjectSpace(a, b)
	return cf(b.Position - a.Position, a.LookVector)
end
function CF.PointToWorldSpace(a, p)
	return a.Position + p
end
function CF.Lerp(a, b, t)
	return cf(a.Position:Lerp(b.Position, t), a.LookVector)
end
function CF.Inverse(a)
	return cf(-a.Position, a.LookVector)
end
function CF.GetComponents(a)
	return a.Position.X, a.Position.Y, a.Position.Z, 1, 0, 0, 0, 1, 0, 0, 0, 1
end
local CFrame = {
	identity = cf(),
	new = function(a, b, c, ...)
		if a == nil then
			return cf()
		end
		if fakeTypeof(a) == "Vector3" then
			if fakeTypeof(b) == "Vector3" then
				local d = b - a
				return cf(a, if d.Magnitude > 0 then d.Unit else v3(0, 0, -1))
			end
			return cf(a)
		end
		return cf(v3(a or 0, b or 0, c or 0))
	end,
	lookAt = function(from, at)
		local d = at - from
		return cf(from, if d.Magnitude > 1e-6 then d.Unit else v3(0, 0, -1))
	end,
	Angles = function()
		return cf()
	end,
	fromEulerAnglesXYZ = function()
		return cf()
	end,
	fromAxisAngle = function()
		return cf()
	end,
}
CFrame.fromOrientation = CFrame.Angles

-- ---------------------------------------------------------- UDim / UDim2 (UI)
local UD = {}
tagged(UD, "UDim")
UD.__index = UD
local function ud(s, o)
	return setmetatable({ Scale = s or 0, Offset = o or 0 }, UD)
end
UD.__add = function(a, b)
	return ud(a.Scale + b.Scale, a.Offset + b.Offset)
end
local UDim = { new = ud }

local UD2 = {}
tagged(UD2, "UDim2")
UD2.__index = UD2
local function ud2(a, b, c, d)
	if fakeTypeof(a) == "UDim" then
		return setmetatable({ X = a, Y = b }, UD2)
	end
	return setmetatable({ X = ud(a, b), Y = ud(c, d) }, UD2)
end
UD2.__add = function(a, b)
	return ud2(a.X + b.X, a.Y + b.Y)
end
UD2.__sub = function(a, b)
	return ud2(ud(a.X.Scale - b.X.Scale, a.X.Offset - b.X.Offset), ud(a.Y.Scale - b.Y.Scale, a.Y.Offset - b.Y.Offset))
end
local UDim2 = {
	new = ud2,
	fromScale = function(x, y)
		return ud2(x, 0, y, 0)
	end,
	fromOffset = function(x, y)
		return ud2(0, x, 0, y)
	end,
}

-- -------------------------------------------------------------------- Random
-- LCG próprio: o gerador do Luau não é garantido entre versões, e o valor deste
-- runner é ser REPETÍVEL. Semente fixa por padrão = mesma partida toda vez.
local RND = {}
tagged(RND, "Random")
RND.__index = RND
local function nextRaw(self)
	self.s = (self.s * 1103515245 + 12345) % 2147483648
	return self.s / 2147483648
end
function RND.NextNumber(self, a, b)
	local r = nextRaw(self)
	if a == nil then
		return r
	end
	return a + r * ((b or 1) - a)
end
function RND.NextInteger(self, a, b)
	return math.floor(a + nextRaw(self) * (b - a + 1 - 1e-9))
end
function RND.NextUnitVector(self)
	local z = nextRaw(self) * 2 - 1
	local t = nextRaw(self) * math.pi * 2
	local r = math.sqrt(math.max(0, 1 - z * z))
	return v3(r * math.cos(t), z, r * math.sin(t))
end
function RND.Clone(self)
	return setmetatable({ s = self.s }, RND)
end
local RANDOM_SEED = 20260818
local Random = {
	new = function(seed)
		RANDOM_SEED += 7919
		return setmetatable({ s = math.floor(tonumber(seed) or RANDOM_SEED) % 2147483648 }, RND)
	end,
}

-- ---------------------------------------------------------------------- Enum
local ENUM_ITEM = {}
tagged(ENUM_ITEM, "EnumItem")
ENUM_ITEM.__index = ENUM_ITEM
ENUM_ITEM.__tostring = function(self)
	return "Enum." .. self.EnumType .. "." .. self.Name
end
local enumCats = {}
local Enum = setmetatable({}, {
	__index = function(_, cat)
		local c = enumCats[cat]
		if not c then
			local items = {}
			c = setmetatable({}, {
				__index = function(_, name)
					local it = items[name]
					if not it then
						it = setmetatable({ Name = name, Value = #items, EnumType = cat }, ENUM_ITEM)
						items[name] = it
					end
					return it
				end,
				__call = function()
					return items
				end,
			})
			enumCats[cat] = c
		end
		return c
	end,
})

-- --------------------------------------------------------------------- Signal
local SIG = {}
tagged(SIG, "RBXScriptSignal")
SIG.__index = SIG

local function newSignal(name)
	return setmetatable({ _name = name, _fns = {}, _waiters = {} }, SIG)
end

function SIG.Connect(self, fn)
	local c = { fn = fn, alive = true }
	table.insert(self._fns, c)
	return {
		Connected = true,
		Disconnect = function()
			c.alive = false
		end,
		disconnect = function()
			c.alive = false
		end,
	}
end
SIG.connect = SIG.Connect
SIG.Once = SIG.Connect

function SIG.Wait(self)
	table.insert(self._waiters, coroutine.running())
	return coroutine.yield()
end

function SIG.Fire(self, ...)
	for _, c in ipairs(table.clone(self._fns)) do
		if c.alive then
			local ok, err = xpcall(c.fn, function(e)
				return tostring(e) .. "\n" .. debug.traceback("", 2)
			end, ...)
			if not ok then
				table.insert(H.errors, { where = "signal " .. tostring(self._name), err = err })
			end
		end
	end
	local w = self._waiters
	if #w > 0 then
		self._waiters = {}
		for _, co in ipairs(w) do
			coroutine.resume(co, ...)
		end
	end
end

-- ------------------------------------------------------------------ Instance
local CLASS_PARENT = {
	Part = "FormFactorPart",
	WedgePart = "FormFactorPart",
	TrussPart = "FormFactorPart",
	MeshPart = "BasePart",
	SpawnLocation = "Part",
	FormFactorPart = "BasePart",
	BasePart = "PVInstance",
	Model = "PVInstance",
	PVInstance = "Instance",
	ModuleScript = "LuaSourceContainer",
	Script = "LuaSourceContainer",
	LocalScript = "Script",
	LuaSourceContainer = "Instance",
	Frame = "GuiObject",
	TextLabel = "GuiObject",
	TextButton = "GuiObject",
	ImageLabel = "GuiObject",
	GuiObject = "GuiBase2d",
	GuiBase2d = "Instance",
	ScreenGui = "LayerCollector",
	BillboardGui = "LayerCollector",
	LayerCollector = "Instance",
	RemoteEvent = "Instance",
	Folder = "Instance",
	Humanoid = "Instance",
}

local SIGNAL_PROPS = {
	Died = 1, HealthChanged = 1, StateChanged = 1, Touched = 1, TouchEnded = 1,
	MoveToFinished = 1, Running = 1, Jumping = 1, Climbing = 1, FreeFalling = 1,
	CharacterAdded = 1, CharacterRemoving = 1, PlayerAdded = 1, PlayerRemoving = 1,
	OnServerEvent = 1, OnClientEvent = 1, Heartbeat = 1, Stepped = 1, RenderStepped = 1,
	PreRender = 1, PreSimulation = 1, PostSimulation = 1, Changed = 1,
	ChildAdded = 1, ChildRemoved = 1, DescendantAdded = 1, DescendantRemoving = 1,
	AncestryChanged = 1, Destroying = 1, Activated = 1, MouseButton1Click = 1,
	InputBegan = 1, InputChanged = 1, InputEnded = 1,
	TouchStarted = 1, TouchMoved = 1, TouchEnded2 = 1, Completed = 1,
	MenuOpened = 1, MenuClosed = 1, Idled = 1, LastInputTypeChanged = 1,
	PlayerConnected = 1, JumpRequest = 1,
}

local INST = {}
tagged(INST, "Instance")
local M = {} -- métodos
local newInstance

local function isA(self, want)
	local c = rawget(self, "_class")
	while c do
		if c == want then
			return true
		end
		c = CLASS_PARENT[c]
	end
	return want == "Instance"
end

INST.__index = function(self, k)
	local m = M[k]
	if m then
		return m
	end
	local p = rawget(self, "_p")
	local v = p[k]
	if v ~= nil then
		return v
	end
	if k == "RootPart" and isA(self, "Humanoid") then
		local par = p.Parent
		return if par then M.FindFirstChild(par, "HumanoidRootPart") else nil
	end
	if k == "ClassName" then
		return rawget(self, "_class")
	end
	if SIGNAL_PROPS[k] then
		local s = newSignal(rawget(self, "_class") .. "." .. k)
		p[k] = s
		return s
	end
	return nil
end

INST.__newindex = function(self, k, v)
	local p = rawget(self, "_p")
	if k == "Parent" then
		local old = p.Parent
		if old then
			local kids = rawget(old, "_kids")
			for i, c in ipairs(kids) do
				if c == self then
					table.remove(kids, i)
					break
				end
			end
		end
		p.Parent = v
		if v then
			table.insert(rawget(v, "_kids"), self)
		end
		return
	end
	if k == "Health" and isA(self, "Humanoid") then
		local maxHp = p.MaxHealth or 100
		local was = p.Health or maxHp
		local now = math.clamp(v, 0, maxHp)
		p.Health = now
		if now ~= was and p.HealthChanged then
			SIG.Fire(p.HealthChanged, now)
		end
		if was > 0 and now <= 0 and not rawget(self, "_dead") then
			rawset(self, "_dead", true)
			if p.Died then
				SIG.Fire(p.Died)
			end
		end
		if now > 0 then
			rawset(self, "_dead", false)
		end
		return
	end
	if k == "Position" and isA(self, "BasePart") then
		p.Position = v
		p.CFrame = cf(v)
		return
	end
	if k == "CFrame" and isA(self, "BasePart") then
		p.CFrame = v
		p.Position = v.Position
		return
	end
	p[k] = v
end

INST.__tostring = function(self)
	return rawget(self, "_p").Name or rawget(self, "_class")
end

-- ------------------------------------------------------------------- métodos
function M.IsA(self, want)
	return isA(self, want)
end

function M.FindFirstChild(self, name, recursive)
	for _, c in ipairs(rawget(self, "_kids")) do
		if rawget(c, "_p").Name == name then
			return c
		end
	end
	if recursive then
		for _, c in ipairs(rawget(self, "_kids")) do
			local f = M.FindFirstChild(c, name, true)
			if f then
				return f
			end
		end
	end
	return nil
end

function M.WaitForChild(self, name)
	-- sem yield: no harness a árvore já está montada antes de qualquer require
	return M.FindFirstChild(self, name)
end

function M.FindFirstChildOfClass(self, class)
	for _, c in ipairs(rawget(self, "_kids")) do
		if rawget(c, "_class") == class then
			return c
		end
	end
	return nil
end

function M.FindFirstChildWhichIsA(self, class)
	for _, c in ipairs(rawget(self, "_kids")) do
		if isA(c, class) then
			return c
		end
	end
	return nil
end

function M.FindFirstAncestorOfClass(self, class)
	local p = rawget(self, "_p").Parent
	while p do
		if rawget(p, "_class") == class then
			return p
		end
		p = rawget(p, "_p").Parent
	end
	return nil
end

function M.GetChildren(self)
	return table.clone(rawget(self, "_kids"))
end

function M.GetDescendants(self, out)
	out = out or {}
	for _, c in ipairs(rawget(self, "_kids")) do
		table.insert(out, c)
		M.GetDescendants(c, out)
	end
	return out
end

function M.ClearAllChildren(self)
	for _, c in ipairs(table.clone(rawget(self, "_kids"))) do
		M.Destroy(c)
	end
end

function M.Destroy(self)
	local p = rawget(self, "_p")
	if p.Destroying then
		SIG.Fire(p.Destroying)
	end
	M.ClearAllChildren(self)
	self.Parent = nil
	rawset(self, "_dead", true)
	p.__destroyed = true
end

function M.Clone(self)
	local c = newInstance(rawget(self, "_class"))
	for k, v in pairs(rawget(self, "_p")) do
		if k ~= "Parent" and fakeTypeof(v) ~= "RBXScriptSignal" then
			rawget(c, "_p")[k] = v
		end
	end
	for _, kid in ipairs(rawget(self, "_kids")) do
		local kc = M.Clone(kid)
		kc.Parent = c
	end
	return c
end

function M.GetAttribute(self, k)
	return rawget(self, "_attr")[k]
end
function M.SetAttribute(self, k, v)
	rawget(self, "_attr")[k] = v
end
function M.GetPropertyChangedSignal(self, k)
	local p = rawget(self, "_p")
	local key = "__sig_" .. k
	if not p[key] then
		p[key] = newSignal(k)
	end
	return p[key]
end

-- --------------------------------------------------------- PVInstance / peças
local function partsOf(inst, out)
	out = out or {}
	if isA(inst, "BasePart") then
		table.insert(out, inst)
	end
	for _, c in ipairs(rawget(inst, "_kids")) do
		partsOf(c, out)
	end
	return out
end

function M.GetPivot(self)
	if isA(self, "BasePart") then
		return rawget(self, "_p").CFrame or cf()
	end
	local pp = rawget(self, "_p").PrimaryPart
	if pp then
		return rawget(pp, "_p").CFrame or cf()
	end
	local all = partsOf(self)
	return if all[1] then rawget(all[1], "_p").CFrame or cf() else cf()
end

function M.PivotTo(self, target)
	local from = M.GetPivot(self).Position
	local delta = target.Position - from
	for _, part in ipairs(partsOf(self)) do
		local pp = rawget(part, "_p")
		pp.Position = (pp.Position or v3(0, 0, 0)) + delta
		pp.CFrame = cf(pp.Position)
	end
	if isA(self, "BasePart") then
		local pp = rawget(self, "_p")
		pp.Position = target.Position
		pp.CFrame = target
	end
end
M.SetPrimaryPartCFrame = M.PivotTo

function M.SetNetworkOwner() end
function M.SetNetworkOwnershipAuto() end
function M.CanSetNetworkOwnership()
	return true
end

-- ------------------------------------------------------------------- Humanoid
function M.MoveTo(self, pos)
	if isA(self, "Humanoid") then
		rawget(self, "_p").__moveTo = pos
	elseif isA(self, "Model") then
		M.PivotTo(self, cf(pos))
	end
end

function M.TakeDamage(self, amount)
	self.Health = (rawget(self, "_p").Health or 0) - amount
end

function M.GetState(self)
	return Enum.HumanoidStateType.Running
end
function M.ChangeState() end
function M.SetStateEnabled() end
function M.Move() end
function M.ApplyDescription() end
function M.GetPlayingAnimationTracks()
	return {}
end
function M.LoadAnimation()
	return { Play = function() end, Stop = function() end, Destroy = function() end }
end

-- ------------------------------------------------------------- RemoteEvent
function M.FireClient(self, _player, payload)
	local n = rawget(self, "_p").Name
	H.lastFire[n] = payload
	H.fireCount[n] = (H.fireCount[n] or 0) + 1
end
function M.FireAllClients(self, payload)
	local n = rawget(self, "_p").Name
	H.lastFire[n] = payload
	H.fireCount[n] = (H.fireCount[n] or 0) + 1
end
function M.FireServer(self, payload)
	local p = rawget(self, "_p")
	if p.OnServerEvent then
		SIG.Fire(p.OnServerEvent, H.localPlayer, payload)
	end
end

-- ------------------------------------------------------------- construtor
local DEFAULTS = {
	Part = function(p)
		p.Size = v3(4, 1, 2)
		p.Position = v3(0, 0, 0)
		p.CFrame = cf()
		p.Transparency = 0
		p.CanCollide = true
		p.Anchored = false
		-- no engine estas NUNCA são nil; deixá-las nil aqui inventaria crash
		p.AssemblyLinearVelocity = v3(0, 0, 0)
		p.AssemblyAngularVelocity = v3(0, 0, 0)
		p.Velocity = v3(0, 0, 0)
		p.Orientation = v3(0, 0, 0)
		p.AssemblyMass = 1
	end,
	Humanoid = function(p)
		p.MaxHealth = 100
		p.Health = 100
		p.WalkSpeed = 16
		p.JumpPower = 50
		p.HipHeight = 0
		p.DisplayName = ""
	end,
}
DEFAULTS.WedgePart = DEFAULTS.Part
DEFAULTS.SpawnLocation = DEFAULTS.Part
DEFAULTS.MeshPart = DEFAULTS.Part
DEFAULTS.TrussPart = DEFAULTS.Part

newInstance = function(class)
	local p = { Name = class }
	local d = DEFAULTS[class]
	if d then
		d(p)
	end
	local inst = setmetatable({ _class = class, _p = p, _kids = {}, _attr = {} }, INST)
	if isA(inst, "BasePart") then
		table.insert(H.parts, inst)
	elseif class == "Humanoid" then
		table.insert(H.humanoids, inst)
	end
	return inst
end

local Instance = { new = newInstance }

-- ---------------------------------------------------- OverlapParams / Raycast
local OP = {}
tagged(OP, "OverlapParams")
OP.__index = OP
local OverlapParams = {
	new = function()
		return setmetatable({ FilterDescendantsInstances = {}, MaxParts = 0 }, OP)
	end,
}
local RP = {}
tagged(RP, "RaycastParams")
RP.__index = RP
local RaycastParams = {
	new = function()
		return setmetatable({ FilterDescendantsInstances = {} }, RP)
	end,
}

-- ------------------------------------------------------------------ serviços
local services = {}
local function service(class, name)
	local s = newInstance(class)
	rawget(s, "_p").Name = name or class
	services[name or class] = s
	return s
end

local Workspace = service("Workspace")
local ReplicatedStorage = service("ReplicatedStorage")
local ServerScriptService = service("ServerScriptService")
local ServerStorage = service("ServerStorage")
local PlayersSvc = service("Players")
local RunService = service("RunService")
local Lighting = service("Lighting")
local Debris = service("Debris")
local TweenService = service("TweenService")
local UserInputService = service("UserInputService")
local GuiService = service("GuiService")
local LocalizationService = service("LocalizationService")
local StarterGui = service("StarterGui")
local StarterPlayer = service("StarterPlayer")
local SoundService = service("SoundService")
local CollectionService = service("CollectionService")
local HttpService = service("HttpService")
local TextService = service("TextService")
local ContextActionService = service("ContextActionService")
local UserGameSettings = service("UserGameSettings")
services.Workspace = Workspace

do -- Workspace
	local p = rawget(Workspace, "_p")
	p.Gravity = 196.2
	p.CurrentCamera = newInstance("Camera")
	rawget(p.CurrentCamera, "_p").CFrame = cf()
	rawget(p.CurrentCamera, "_p").ViewportSize = v2(1280, 720)
end

function M.GetPartBoundsInRadius(self, pos, radius, params)
	local excl = {}
	if params then
		for _, e in ipairs(params.FilterDescendantsInstances or {}) do
			excl[e] = true
		end
	end
	local out = {}
	for _, part in ipairs(H.parts) do
		local pp = rawget(part, "_p")
		if not pp.__destroyed and pp.Position and (pp.Position - pos).Magnitude <= radius then
			local anc, skip = pp.Parent, false
			while anc do
				if excl[anc] then
					skip = true
					break
				end
				anc = rawget(anc, "_p").Parent
			end
			if not skip and pp.Parent then
				table.insert(out, part)
			end
		end
	end
	return out
end
M.GetPartBoundsInBox = M.GetPartBoundsInRadius
function M.Raycast()
	return nil
end

do -- RunService
	local p = rawget(RunService, "_p")
	p.Heartbeat = newSignal("Heartbeat")
	p.Stepped = newSignal("Stepped")
	p.RenderStepped = newSignal("RenderStepped")
	p.PreRender = newSignal("PreRender")
	p.PostSimulation = p.Heartbeat
end
function M.IsServer()
	return true
end
function M.IsClient()
	return false
end
function M.IsStudio()
	return false
end
function M.IsRunning()
	return true
end
function M.BindToRenderStep() end
function M.UnbindFromRenderStep() end

-- Debris
local timers = {}
function M.AddItem(_, inst, delay)
	table.insert(timers, { at = NOW + (delay or 0), fn = function()
		M.Destroy(inst)
	end })
end

-- TweenService
function M.Create(_, obj, _info, goal)
	return {
		Play = function()
			for k, val in pairs(goal) do
				obj[k] = val
			end
		end,
		Cancel = function() end,
		Pause = function() end,
		Destroy = function() end,
		Completed = newSignal("Tween.Completed"),
	}
end

-- UserInputService / GuiService / LocalizationService
do
	local p = rawget(UserInputService, "_p")
	p.TouchEnabled = false
	p.KeyboardEnabled = true
	p.MouseEnabled = true
	p.GamepadEnabled = false
	local g = rawget(GuiService, "_p")
	g.PreferredTransparency = 1
	g.ReducedMotionEnabled = false
	g.PreferredTextSize = Enum.PreferredTextSize.Medium
	rawget(LocalizationService, "_p").RobloxLocaleId = "pt-br"
	rawget(LocalizationService, "_p").SystemLocaleId = "pt-br"
	rawget(UserGameSettings, "_p").SavedQualityLevel = Enum.SavedQualitySetting.Automatic
end
function M.GetMouseLocation()
	return v2(640, 360)
end
function M.GetGuiInset()
	return v2(0, 36), v2(0, 0)
end
function M.GetFocusedTextBox()
	return nil
end
function M.GetTagged()
	return {}
end
function M.JSONEncode(_, t)
	return tostring(t)
end
function M.GenerateGUID()
	return "harness-guid"
end
function M.SetCore() end
function M.GetCore()
	return nil
end

-- --------------------------------------------------------------------- Players
local playerList = {}
do
	local p = rawget(PlayersSvc, "_p")
	p.CharacterAutoLoads = true
	p.MaxPlayers = 24
end

function M.GetPlayers()
	return table.clone(playerList)
end
function M.GetPlayerFromCharacter(_, char)
	for _, pl in ipairs(playerList) do
		if rawget(pl, "_p").Character == char then
			return pl
		end
	end
	return nil
end
function M.GetPlayerByUserId(_, id)
	for _, pl in ipairs(playerList) do
		if rawget(pl, "_p").UserId == id then
			return pl
		end
	end
	return nil
end

--- Corpo R6 mínimo: HumanoidRootPart + Humanoid é tudo que o servidor lê.
local function buildCharacter(name)
	local model = newInstance("Model")
	model.Name = name
	local root = newInstance("Part")
	root.Name = "HumanoidRootPart"
	root.Size = v3(2, 2, 1)
	root.Position = v3(0, 4, 0)
	root.Parent = model
	local torso = newInstance("Part")
	torso.Name = "Torso"
	torso.Position = v3(0, 4, 0)
	torso.Parent = model
	local head = newInstance("Part")
	head.Name = "Head"
	head.Size = v3(1.4, 1.4, 1.4)
	head.Position = v3(0, 5.5, 0)
	head.Parent = model
	local hum = newInstance("Humanoid")
	hum.Name = "Humanoid"
	hum.Parent = model
	model.PrimaryPart = root
	return model
end

function M.LoadCharacter(self)
	local p = rawget(self, "_p")
	local old = p.Character
	if old then
		M.Destroy(old)
	end
	local char = buildCharacter(p.Name)
	char.Parent = Workspace
	p.Character = char
	if p.CharacterAdded then
		SIG.Fire(p.CharacterAdded, char)
	end
	return char
end
function M.Kick() end
function M.GetRankInGroup()
	return 0
end

--- Entra um jogador falso (o runner chama). Dispara PlayerAdded como o engine.
function H.addPlayer(name, userId)
	local pl = newInstance("Player")
	local p = rawget(pl, "_p")
	p.Name = name
	p.DisplayName = name
	p.UserId = userId
	p.Parent = PlayersSvc
	table.insert(rawget(PlayersSvc, "_kids"), pl)
	table.insert(playerList, pl)
	local svcp = rawget(PlayersSvc, "_p")
	if svcp.PlayerAdded then
		SIG.Fire(svcp.PlayerAdded, pl)
	end
	return pl
end

function H.removePlayer(pl)
	for i, x in ipairs(playerList) do
		if x == pl then
			table.remove(playerList, i)
			break
		end
	end
	local svcp = rawget(PlayersSvc, "_p")
	if svcp.PlayerRemoving then
		SIG.Fire(svcp.PlayerRemoving, pl)
	end
end

-- ----------------------------------------------------------------------- game
local game = setmetatable({}, {
	__index = function(_, k)
		if k == "GetService" or k == "FindService" then
			return function(_, name)
				local s = services[name]
				if not s then
					s = service(name)
				end
				return s
			end
		elseif k == "BindToClose" then
			return function() end
		elseif k == "PlaceId" or k == "JobId" then
			return 0
		end
		return services[k]
	end,
})

-- ----------------------------------------------------------------------- task
local deferred = {}
local function spawnNow(fn, ...)
	local co = coroutine.create(fn)
	local ok, err = coroutine.resume(co, ...)
	if not ok then
		table.insert(H.errors, { where = "task.spawn", err = tostring(err) })
	end
	return co
end

local task = {
	spawn = spawnNow,
	defer = function(fn, ...)
		table.insert(deferred, { fn = fn, args = table.pack(...) })
	end,
	delay = function(t, fn, ...)
		local args = table.pack(...)
		table.insert(timers, { at = NOW + (t or 0), fn = function()
			spawnNow(fn, table.unpack(args, 1, args.n))
		end })
	end,
	wait = function(t)
		return t or 0
	end,
	cancel = function() end,
	desynchronize = function() end,
	synchronize = function() end,
}

--- Avança o relógio virtual e drena defer/delay/Debris. O runner chama isto
--- ANTES dos step() do frame, como o engine faria entre Heartbeats.
function H.tick(dt)
	NOW += dt
	local rounds = 0
	while #deferred > 0 and rounds < 32 do
		rounds += 1
		local batch = deferred
		deferred = {}
		for _, d in ipairs(batch) do
			spawnNow(d.fn, table.unpack(d.args, 1, d.args.n))
		end
	end
	local keep = {}
	for _, t in ipairs(timers) do
		if t.at <= NOW then
			local ok, err = pcall(t.fn)
			if not ok then
				table.insert(H.errors, { where = "timer", err = tostring(err) })
			end
		else
			table.insert(keep, t)
		end
	end
	timers = keep
end

--- Locomoção de araque: leva cada Humanoid ao alvo do último MoveTo, na
--- WalkSpeed dele. É o que faz bot ANDAR (e portanto a zona pegar quem fica).
--- ponytail: TETO CONHECIDO — sem colisão nem gravidade; se um teste precisar
--- de "esbarrar na rocha", isso tem de virar consulta ao Terrain aqui.
function H.physics(dt)
	for _, hum in ipairs(H.humanoids) do
		local p = rawget(hum, "_p")
		local target = p.__moveTo
		if target and (p.Health or 0) > 0 and not p.__destroyed then
			local model = p.Parent
			if model and rawget(model, "_p").Parent then
				local root = M.FindFirstChild(model, "HumanoidRootPart")
				if root then
					local pos = rawget(root, "_p").Position
					local d = v3(target.X - pos.X, 0, target.Z - pos.Z)
					local dist = d.Magnitude
					local stepLen = (p.WalkSpeed or 16) * dt
					if dist <= stepLen or dist < 0.05 then
						p.__moveTo = nil
						if dist > 0.05 then
							M.PivotTo(model, cf(v3(target.X, pos.Y, target.Z)))
						end
					else
						M.PivotTo(model, cf(pos + d.Unit * stepLen))
					end
				end
			end
		end
	end
end

-- ------------------------------------------------------- carregador de módulos
local cache = {}
local GLOBALS

local function fakeRequire(target)
	if type(target) == "string" then
		error("[harness] require por string não existe no Roblox: " .. target, 2)
	end
	if target == nil then
		error("[harness] require(nil) — módulo não encontrado na árvore", 2)
	end
	if cache[target] ~= nil then
		return cache[target]
	end
	local src = rawget(target, "_source")
	if not src then
		error("[harness] " .. tostring(target) .. " não é um ModuleScript do projeto", 2)
	end
	local path = rawget(target, "_path")
	local chunk, perr = loadstring(src, "@" .. path)
	if not chunk then
		error("[harness] erro de sintaxe em " .. path .. ": " .. tostring(perr), 2)
	end
	local env = setmetatable({ script = target }, { __index = GLOBALS })
	setfenv(chunk, env)
	local result = chunk()
	cache[target] = if result == nil then true else result
	return cache[target]
end

local fakeOs = setmetatable({
	clock = function()
		return NOW
	end,
	time = function()
		return 1787000000 + math.floor(NOW)
	end,
}, { __index = os })

GLOBALS = setmetatable({
	game = game,
	workspace = Workspace,
	Workspace = Workspace,
	Instance = Instance,
	Vector3 = Vector3,
	Vector2 = Vector2,
	CFrame = CFrame,
	Color3 = Color3,
	UDim = UDim,
	UDim2 = UDim2,
	Random = Random,
	Enum = Enum,
	OverlapParams = OverlapParams,
	RaycastParams = RaycastParams,
	typeof = fakeTypeof,
	require = fakeRequire,
	task = task,
	os = fakeOs,
	tick = function()
		return NOW
	end,
	time = function()
		return NOW
	end,
	wait = function(t)
		return t or 0
	end,
	spawn = spawnNow,
	delay = task.delay,
	warn = function(...)
		local parts = {}
		for i = 1, select("#", ...) do
			parts[i] = tostring(select(i, ...))
		end
		print("[warn] " .. table.concat(parts, " "))
	end,
	settings = function()
		return { GetService = function(_, n)
			return services[n] or service(n)
		end }
	end,
	UserSettings = function()
		return { GetService = function(_, n)
			return services[n] or service(n)
		end }
	end,
	TweenInfo = { new = function(...)
		return { ... }
	end },
	NumberRange = { new = function(a, b)
		return { Min = a, Max = b or a }
	end },
	NumberSequence = { new = function(...)
		return { ... }
	end },
	NumberSequenceKeypoint = { new = function(...)
		return { ... }
	end },
	ColorSequence = { new = function(...)
		return { ... }
	end },
	ColorSequenceKeypoint = { new = function(...)
		return { ... }
	end },
	BrickColor = { new = function()
		return { Color = c3(1, 1, 1) }
	end },
	Rect = { new = function(...)
		return { ... }
	end },
	Font = {
		new = function(...)
			return { ... }
		end,
		fromEnum = function(e)
			return { e }
		end,
	},
	Ray = { new = function(o, d)
		return { Origin = o, Direction = d }
	end },
}, { __index = BASE })

H.GLOBALS = GLOBALS
H.game = game
H.services = services
H.Vector3 = Vector3
H.CFrame = CFrame
H.Color3 = Color3
H.Instance = Instance
H.Enum = Enum
H.typeof = fakeTypeof
H.require = fakeRequire
H.Players = PlayersSvc
H.RunService = RunService
H.Workspace = Workspace
H.task = task

-- ---------------------------------------------------- montagem da árvore Rojo
--- Espelha roblox/default.project.json: shared → ReplicatedStorage.Shared,
--- server → ServerScriptService.Server, client → StarterPlayerScripts.Client
--- (também visível em Players.LocalPlayer.PlayerScripts.Client, como no jogo).
local function mount(parent, folderName, prefix)
	local folder = newInstance("Folder")
	folder.Name = folderName
	folder.Parent = parent
	for path, src in pairs(SOURCES) do
		local rest = path:match("^" .. prefix .. "/(.+)$")
		if rest then
			local name, kind = rest:match("^([%w_]+)%.?(%a*)%.luau$")
			if name then
				local class = if kind == "server" or kind == "client" then "Script" else "ModuleScript"
				local mod = newInstance(class)
				mod.Name = name
				rawset(mod, "_source", src)
				rawset(mod, "_path", path)
				mod.Parent = folder
			end
		end
	end
	return folder
end

function H.buildTree()
	H.Shared = mount(ReplicatedStorage, "Shared", "shared")
	H.Server = mount(ServerScriptService, "Server", "server")

	local starterScripts = newInstance("StarterPlayerScripts")
	starterScripts.Name = "StarterPlayerScripts"
	starterScripts.Parent = StarterPlayer
	H.Client = mount(starterScripts, "Client", "client")

	-- LocalPlayer com PlayerScripts apontando para a MESMA pasta de módulos
	local lp = newInstance("Player")
	rawget(lp, "_p").Name = "DiretorLocal"
	rawget(lp, "_p").DisplayName = "DiretorLocal"
	rawget(lp, "_p").UserId = 1
	local ps = newInstance("PlayerScripts")
	ps.Name = "PlayerScripts"
	ps.Parent = lp
	H.Client.Parent = ps
	lp.Parent = PlayersSvc
	rawget(PlayersSvc, "_p").LocalPlayer = lp
	H.localPlayer = lp
	return H
end

return H
