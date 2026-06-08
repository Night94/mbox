(function () {
  var WORLD_WIDTH = 200;
  var WORLD_HEIGHT = 150;
  var PIXEL_WIDTH = 5;
  var PIXEL_HEIGHT = 4;
  var GAP = 1;
  var TICK_MS = 90;
  var WORLD_STATE_KEY = "web-pixel.world.config.v1";
  var DEBUG_LOG_KEY = "web-pixel.debug.v1";
  var DEBUG_LOG_LIMIT = 200;
  var CENTER_COLOR = "#fff";
  var PILL_COLOR = "#ffd400";
  var DEFAULT_POWER = 100;
  var PILL_POWER = 10;
  var MOMENTUM_DECAY = 0.96;
  var POWER_VECTOR_ACCELERATION = 0.2;
  var THRUST_POWER_COST = 0.5;
  var MOVE_COST_INTERVAL = 10;
  var LOOK_RADIUS = 40;
  var LOOK_POWER_COST = 4;
  var EXL_LAG_CANDIDATE_RATIO = 0.5;
  var EXL_LAG_CHANCE = 0.5;
  var PILLS_PER_EXL = 10;
  var MAX_EXLS = 20;
  var registeredApps = Object.create(null);

  function loadJson(key, fallback) {
    try {
      var text = window.localStorage.getItem(key);
      return text ? JSON.parse(text) : fallback;
    } catch (error) {
      return fallback;
    }
  }

  function saveJson(key, value) {
    window.localStorage.setItem(key, JSON.stringify(value));
  }

  function appendDebug(entry) {
    var log = loadJson(DEBUG_LOG_KEY, []);
    if (!Array.isArray(log)) {
      log = [];
    }
    log.push(entry);
    if (log.length > DEBUG_LOG_LIMIT) {
      log = log.slice(log.length - DEBUG_LOG_LIMIT);
    }
    saveJson(DEBUG_LOG_KEY, log);
  }

  function keyFor(point) {
    return point.x + "," + point.y;
  }

  function inBounds(point) {
    return point.x >= 0 && point.y >= 0 && point.x < WORLD_WIDTH && point.y < WORLD_HEIGHT;
  }

  function absoluteExls(bui) {
    return bui.exls.map(function (exl) {
      return {
        x: bui.center.x + exl.dx,
        y: bui.center.y + exl.dy,
        dx: exl.dx,
        dy: exl.dy
      };
    });
  }

  function bodyPoints(bui) {
    return [bui.center].concat(absoluteExls(bui).map(function (exl) {
      return { x: exl.x, y: exl.y };
    }));
  }

  function buildBodySet(state) {
    var occupied = Object.create(null);
    Object.keys(state.apps).forEach(function (appId) {
      bodyPoints(state.apps[appId].bui).forEach(function (point) {
        occupied[keyFor(point)] = appId;
      });
    });
    return occupied;
  }

  function buildOccupied(state, excludeAppId) {
    var occupied = Object.create(null);
    Object.keys(state.apps).forEach(function (appId) {
      if (appId === excludeAppId) {
        return;
      }

      bodyPoints(state.apps[appId].bui).forEach(function (point) {
        occupied[keyFor(point)] = appId;
      });
    });
    return occupied;
  }

  function randomInt(max) {
    return Math.floor(Math.random() * max);
  }

  function randomItem(items) {
    return items[randomInt(items.length)];
  }

  function shuffledIndexes(length) {
    var indexes = [];
    for (var index = 0; index < length; index += 1) {
      indexes.push(index);
    }
    for (var cursor = indexes.length - 1; cursor > 0; cursor -= 1) {
      var swapIndex = randomInt(cursor + 1);
      var value = indexes[cursor];
      indexes[cursor] = indexes[swapIndex];
      indexes[swapIndex] = value;
    }
    return indexes;
  }

  function randomFullSpeed() {
    var angle = Math.random() * Math.PI * 2;
    return {
      x: Math.cos(angle),
      y: Math.sin(angle)
    };
  }

  function vectorMagnitude(vector) {
    var x = Number(vector && vector.x) || 0;
    var y = Number(vector && vector.y) || 0;
    return Math.sqrt((x * x) + (y * y));
  }

  function hasNumericVector(vector) {
    return Boolean(vector) && Number.isFinite(Number(vector.x)) && Number.isFinite(Number(vector.y));
  }

  function capVector(vector, maxMagnitude) {
    var x = Number(vector && vector.x) || 0;
    var y = Number(vector && vector.y) || 0;
    var magnitude = Math.sqrt((x * x) + (y * y));
    var cap = maxMagnitude == null ? 1 : maxMagnitude;

    if (magnitude > cap && magnitude > 0) {
      x = (x / magnitude) * cap;
      y = (y / magnitude) * cap;
    }

    return {
      x: Math.max(-cap, Math.min(cap, x)),
      y: Math.max(-cap, Math.min(cap, y))
    };
  }

  function createMotion() {
    return {
      carryX: 0,
      carryY: 0
    };
  }

  function neighbors(point) {
    var result = [];
    for (var dy = -1; dy <= 1; dy += 1) {
      for (var dx = -1; dx <= 1; dx += 1) {
        if (dx !== 0 || dy !== 0) {
          result.push({ x: point.x + dx, y: point.y + dy });
        }
      }
    }
    return result;
  }

  function isConnected(center, exls) {
    var points = [center].concat(exls.map(function (exl) {
      return { x: center.x + exl.dx, y: center.y + exl.dy };
    }));
    var pointSet = Object.create(null);
    var seen = Object.create(null);
    var queue = [center];

    points.forEach(function (point) {
      pointSet[keyFor(point)] = true;
    });

    seen[keyFor(center)] = true;

    while (queue.length > 0) {
      neighbors(queue.shift()).forEach(function (neighbor) {
        var key = keyFor(neighbor);
        if (pointSet[key] && !seen[key]) {
          seen[key] = true;
          queue.push(neighbor);
        }
      });
    }

    return points.every(function (point) {
      return Boolean(seen[keyFor(point)]);
    });
  }

  function isValidBui(bui) {
    var taken = Object.create(null);
    if (!bui || !inBounds(bui.center) || !Array.isArray(bui.exls)) {
      return false;
    }

    if (!isConnected(bui.center, bui.exls)) {
      return false;
    }

    return bodyPoints(bui).every(function (point) {
      var key = keyFor(point);
      if (!inBounds(point) || taken[key]) {
        return false;
      }
      taken[key] = true;
      return true;
    });
  }

  function createInitialBui(state, appId) {
    var otherOccupied = buildOccupied(state, appId);

    for (var attempt = 0; attempt < 2000; attempt += 1) {
      var center = {
        x: randomInt(WORLD_WIDTH),
        y: randomInt(WORLD_HEIGHT)
      };
      if (otherOccupied[keyFor(center)]) {
        continue;
      }

      var exlPoints = [];
      var localOccupied = Object.create(null);
      localOccupied[keyFor(center)] = true;

      while (exlPoints.length < 10) {
        var anchors = [center].concat(exlPoints);
        var candidates = [];
        anchors.forEach(function (anchor) {
          neighbors(anchor).forEach(function (candidate) {
            var key = keyFor(candidate);
            if (inBounds(candidate) && !localOccupied[key] && !otherOccupied[key]) {
              candidates.push(candidate);
            }
          });
        });

        if (candidates.length === 0) {
          break;
        }

        var picked = randomItem(candidates);
        localOccupied[keyFor(picked)] = true;
        exlPoints.push(picked);
      }

      if (exlPoints.length === 10) {
        return {
          center: center,
          exls: exlPoints.map(function (point) {
            return { dx: point.x - center.x, dy: point.y - center.y };
          })
        };
      }
    }

    throw new Error("Could not place initial bui for " + appId);
  }

  function createInitialAppState(state, appId) {
    return {
      active: true,
      bui: createInitialBui(state, appId),
      momentum: randomFullSpeed(),
      motion: createMotion(),
      power: DEFAULT_POWER,
      powerCarry: 0,
      moveCostMoves: 0,
      lastPowerDelta: 0,
      lastPillsCollected: 0,
      pillsSwallowed: 0
    };
  }

  function normalizePills(state) {
    var occupied = buildBodySet(state);
    var seen = Object.create(null);
    if (!Array.isArray(state.pills)) {
      state.pills = [];
      return;
    }

    state.pills = state.pills.filter(function (pill) {
      var point = { x: Number(pill && pill.x), y: Number(pill && pill.y) };
      var key = keyFor(point);
      if (!inBounds(point) || seen[key] || occupied[key]) {
        return false;
      }
      seen[key] = true;
      pill.x = point.x;
      pill.y = point.y;
      return true;
    });
  }

  function addPills(state, count) {
    var occupied = buildBodySet(state);
    var pillKeys = Object.create(null);
    var added = 0;
    var attempts = 0;

    state.pills.forEach(function (pill) {
      pillKeys[keyFor(pill)] = true;
    });

    while (added < count && attempts < count * 100) {
      var pill = {
        x: randomInt(WORLD_WIDTH),
        y: randomInt(WORLD_HEIGHT)
      };
      var key = keyFor(pill);
      attempts += 1;
      if (!occupied[key] && !pillKeys[key]) {
        state.pills.push(pill);
        pillKeys[key] = true;
        added += 1;
      }
    }
  }

  function createRenderer(element, colorsByApp) {
    var rendered = Object.create(null);

    element.style.width = ((WORLD_WIDTH * PIXEL_WIDTH) + ((WORLD_WIDTH - 1) * GAP)) + "px";
    element.style.height = ((WORLD_HEIGHT * PIXEL_HEIGHT) + ((WORLD_HEIGHT - 1) * GAP)) + "px";

    function drawPixel(point, owner, color, kind) {
      var key = keyFor(point);
      var pixel = rendered[key];
      if (!pixel) {
        pixel = document.createElement("div");
        pixel.className = "world-pixel";
        pixel.style.left = (point.x * (PIXEL_WIDTH + GAP)) + "px";
        pixel.style.top = (point.y * (PIXEL_HEIGHT + GAP)) + "px";
        pixel.style.width = PIXEL_WIDTH + "px";
        pixel.style.height = PIXEL_HEIGHT + "px";
        rendered[key] = pixel;
        element.appendChild(pixel);
      }
      pixel.dataset.owner = owner;
      pixel.dataset.kind = kind;
      pixel.style.backgroundColor = color;
    }

    function render(state) {
      var next = Object.create(null);

      state.pills.forEach(function (pill) {
        next[keyFor(pill)] = {
          point: pill,
          owner: "world",
          kind: "pill",
          color: PILL_COLOR
        };
      });

      Object.keys(state.apps).forEach(function (appId) {
        absoluteExls(state.apps[appId].bui).forEach(function (point) {
          next[keyFor(point)] = {
            point: point,
            owner: appId,
            kind: "exl",
            color: colorsByApp[appId] || "#fff"
          };
        });

        next[keyFor(state.apps[appId].bui.center)] = {
          point: state.apps[appId].bui.center,
          owner: appId,
          kind: "center",
          color: CENTER_COLOR
        };
      });

      Object.keys(next).forEach(function (key) {
        drawPixel(next[key].point, next[key].owner, next[key].color, next[key].kind);
      });

      Object.keys(rendered).forEach(function (key) {
        if (!next[key]) {
          rendered[key].remove();
          delete rendered[key];
        }
      });
    }

    function showLook(center, radius, color) {
      var overlay = document.createElement("div");
      var pitchX = PIXEL_WIDTH + GAP;
      var pitchY = PIXEL_HEIGHT + GAP;

      overlay.className = "look-overlay";
      overlay.style.borderColor = color || "#fff";
      overlay.style.color = color || "#fff";
      overlay.style.left = ((center.x * pitchX) + (PIXEL_WIDTH / 2) - (radius * pitchX)) + "px";
      overlay.style.top = ((center.y * pitchY) + (PIXEL_HEIGHT / 2) - (radius * pitchY)) + "px";
      overlay.style.width = (radius * 2 * pitchX) + "px";
      overlay.style.height = (radius * 2 * pitchY) + "px";
      element.appendChild(overlay);
      window.setTimeout(function () {
        overlay.remove();
      }, 1000);
    }

    return {
      render: render,
      showLook: showLook
    };
  }

  function updateControlReadouts(refs, state) {
    Object.keys(refs).forEach(function (appId) {
      var appState = state.apps[appId];
      refs[appId].power.textContent = "power " + Math.floor(appState.power);
      refs[appId].button.textContent = appId + (appState.active ? " on" : " off");
      refs[appId].button.setAttribute("aria-pressed", appState.active ? "true" : "false");
    });
  }

  function normalizeAppRuntimeState(appState) {
    if (!Number.isFinite(Number(appState.power))) {
      appState.power = DEFAULT_POWER;
    }
    if (!Number.isFinite(Number(appState.powerCarry))) {
      appState.powerCarry = 0;
    }
    if (!Number.isFinite(Number(appState.moveCostMoves))) {
      appState.moveCostMoves = Number.isFinite(Number(appState.coastMoves)) ? Number(appState.coastMoves) : 0;
    }
    delete appState.coastMoves;
    if (!Number.isFinite(Number(appState.lastPowerDelta))) {
      appState.lastPowerDelta = 0;
    }
    if (!Number.isFinite(Number(appState.lastPillsCollected))) {
      appState.lastPillsCollected = 0;
    }
    if (!Number.isFinite(Number(appState.pillsSwallowed))) {
      appState.pillsSwallowed = 0;
    }
    if (!hasNumericVector(appState.momentum)) {
      appState.momentum = hasNumericVector(appState.speed) ? capVector(appState.speed) : randomFullSpeed();
    } else {
      appState.momentum = capVector(appState.momentum);
    }
  }

  function growOneExl(state, appId) {
    var appState = state.apps[appId];
    var bui = appState.bui;
    var occupied = buildOccupied(state, appId);
    var localOccupied = Object.create(null);
    var pillOccupied = Object.create(null);
    var candidates = [];

    bodyPoints(bui).forEach(function (point) {
      localOccupied[keyFor(point)] = true;
    });

    state.pills.forEach(function (pill) {
      pillOccupied[keyFor(pill)] = true;
    });

    bodyPoints(bui).forEach(function (anchor) {
      neighbors(anchor).forEach(function (candidate) {
        var key = keyFor(candidate);
        var candidateExls;

        if (!inBounds(candidate) || localOccupied[key] || occupied[key] || pillOccupied[key]) {
          return;
        }

        candidateExls = bui.exls.concat({
          dx: candidate.x - bui.center.x,
          dy: candidate.y - bui.center.y
        });

        if (isValidBui({ center: bui.center, exls: candidateExls })) {
          candidates.push({ x: candidate.x, y: candidate.y });
          localOccupied[key] = true;
        }
      });
    });

    if (candidates.length === 0) {
      return false;
    }

    var picked = randomItem(candidates);
    bui.exls.push({
      dx: picked.x - bui.center.x,
      dy: picked.y - bui.center.y
    });
    return true;
  }

  function growEarnedExls(state, appId) {
    var appState = state.apps[appId];
    var targetExlCount = Math.min(MAX_EXLS, 10 + Math.floor(appState.pillsSwallowed / PILLS_PER_EXL));

    while (appState.bui.exls.length < targetExlCount) {
      if (!growOneExl(state, appId)) {
        return;
      }
    }
  }

  function collectPills(state, appId) {
    var appState = state.apps[appId];
    var collected = Object.create(null);
    var gained = 0;
    var swallowed = 0;

    absoluteExls(appState.bui).forEach(function (exl) {
      collected[keyFor(exl)] = true;
    });

    state.pills = state.pills.filter(function (pill) {
      if (collected[keyFor(pill)]) {
        gained += PILL_POWER;
        swallowed += 1;
        return false;
      }
      return true;
    });

    if (gained > 0) {
      appState.power += gained;
      appState.lastPowerDelta += gained;
      appState.lastPillsCollected += swallowed;
      appState.pillsSwallowed += swallowed;
      growEarnedExls(state, appId);
      addPills(state, swallowed);
    }
  }

  function createAppStateStore(appId) {
    var key = "web-pixel.app." + appId + ".state.v1";
    return {
      load: function () {
        return loadJson(key, {});
      },
      save: function (state) {
        saveJson(key, state || {});
      }
    };
  }

  function createWorld(options) {
    var apps = options.apps || [];
    var state = loadJson(WORLD_STATE_KEY, { tick: 0, apps: {} });
    var manifestIds = Object.create(null);
    var colorsByApp = Object.create(null);
    var controlRefs = Object.create(null);
    var powerApplied = Object.create(null);
    var exlMoveApplied = Object.create(null);
    var lookApplied = Object.create(null);
    var renderer;
    var running = false;
    var tickTimer = null;
    var turboMode = false;

    apps.forEach(function (app) {
      manifestIds[app.id] = true;
      colorsByApp[app.id] = app.color || "#fff";
    });

    renderer = createRenderer(options.element, colorsByApp);
    normalizePills(state);

    Object.keys(state.apps).forEach(function (appId) {
      if (!manifestIds[appId]) {
        delete state.apps[appId];
      } else if (!isValidBui(state.apps[appId].bui)) {
        delete state.apps[appId];
      }
    });

    apps.forEach(function (app) {
      if (!state.apps[app.id]) {
        state.apps[app.id] = createInitialAppState(state, app.id);
      } else {
        if (!hasNumericVector({ x: state.apps[app.id].motion && state.apps[app.id].motion.carryX, y: state.apps[app.id].motion && state.apps[app.id].motion.carryY })) {
          state.apps[app.id].motion = createMotion();
        }
        normalizeAppRuntimeState(state.apps[app.id]);
      }
    });

    normalizePills(state);

    saveJson(WORLD_STATE_KEY, state);
    renderer.render(state);

    function persistAndRender() {
      saveJson(WORLD_STATE_KEY, state);
      renderer.render(state);
      if (options.tickReadout) {
        options.tickReadout.textContent = "tick " + state.tick;
      }
      updateControlReadouts(controlRefs, state);
      updateTurboButton();
    }

    function updateTurboButton() {
      if (!options.turboButton) {
        return;
      }
      options.turboButton.textContent = turboMode ? "turbo on" : "turbo";
      options.turboButton.setAttribute("aria-pressed", turboMode ? "true" : "false");
    }

    function poweredBuiCount() {
      return Object.keys(state.apps).filter(function (appId) {
        return Number(state.apps[appId].power) > 0;
      }).length;
    }

    function deactivateUnpowered() {
      Object.keys(state.apps).forEach(function (appId) {
        var appState = state.apps[appId];
        if (Number(appState.power) <= 0) {
          appState.power = 0;
          appState.active = false;
        }
      });
    }

    function setTurboMode(active) {
      turboMode = Boolean(active);
      updateTurboButton();
    }

    function setActive(appId, active) {
      state.apps[appId].active = active;
      persistAndRender();
      renderControls();
    }

    function resetWorld() {
      state.apps = {};
      state.pills = [];
      state.tick = 0;
      apps.forEach(function (app) {
        state.apps[app.id] = createInitialAppState(state, app.id);
      });
      persistAndRender();
      renderControls();
    }

    function renderControls() {
      options.controlsElement.replaceChildren();
      controlRefs = Object.create(null);
      apps.forEach(function (app) {
        var panel = document.createElement("div");
        var power = document.createElement("div");
        var button = document.createElement("button");
        var appState = state.apps[app.id];

        panel.className = "app-panel";

        power.className = "power-readout";

        button.type = "button";
        button.className = "app-toggle";
        button.addEventListener("click", function () {
          setActive(app.id, !appState.active);
        });

        panel.appendChild(power);
        panel.appendChild(button);
        options.controlsElement.appendChild(panel);

        controlRefs[app.id] = {
          power: power,
          button: button
        };
      });
      updateControlReadouts(controlRefs, state);
    }

    function reflectMomentumFromBounds(appState, step, attemptedCenter, attemptedPoints) {
      var reflected = {
        x: appState.momentum.x,
        y: appState.momentum.y
      };
      var allAttemptedPoints = [attemptedCenter].concat(attemptedPoints || []);

      if (allAttemptedPoints.some(function (point) {
        return point.x < 0 || point.x >= WORLD_WIDTH;
      })) {
        reflected.x *= -1;
      }

      if (allAttemptedPoints.some(function (point) {
        return point.y < 0 || point.y >= WORLD_HEIGHT;
      })) {
        reflected.y *= -1;
      }

      if (step.x !== 0 || step.y !== 0) {
        appState.momentum = reflected;
        appState.motion = createMotion();
      }
    }

    function classifyInvalidExlPoints(center, exlPoints, occupied) {
      var nextExls;

      if (exlPoints.some(function (point) {
        return !inBounds(point);
      })) {
        return "body-out-of-bounds";
      }

      if (exlPoints.some(function (point) {
        return occupied[keyFor(point)];
      })) {
        return "exl-occupied";
      }

      nextExls = exlPoints.map(function (point) {
        return {
          dx: point.x - center.x,
          dy: point.y - center.y
        };
      });

      return isValidBui({ center: center, exls: nextExls }) ? null : "body-disconnected";
    }

    function validExlPoints(center, exlPoints, occupied) {
      var nextExls;

      if (exlPoints.some(function (point) {
        return !inBounds(point) || occupied[keyFor(point)];
      })) {
        return null;
      }

      nextExls = exlPoints.map(function (point) {
        return {
          dx: point.x - center.x,
          dy: point.y - center.y
        };
      });

      return isValidBui({ center: center, exls: nextExls }) ? nextExls : null;
    }

    function applyExlLag(previousExls, finalExlPoints, requestedCenter, occupied) {
      var indexes = shuffledIndexes(previousExls.length);
      var candidates = indexes.slice(0, Math.floor(previousExls.length * EXL_LAG_CANDIDATE_RATIO));

      candidates.forEach(function (lagIndex) {
        var candidatePoints;

        if (Math.random() >= EXL_LAG_CHANCE) {
          return;
        }

        candidatePoints = finalExlPoints.map(function (point) {
          return { x: point.x, y: point.y };
        });

        candidatePoints[lagIndex] = {
          x: previousExls[lagIndex].x,
          y: previousExls[lagIndex].y
        };

        if (validExlPoints(requestedCenter, candidatePoints, occupied)) {
          finalExlPoints = candidatePoints;
        }
      });

      return finalExlPoints;
    }

    function moveStep(appId, step) {
      var appState = state.apps[appId];
      var bui = appState.bui;
      var previousCenter = { x: bui.center.x, y: bui.center.y };
      var previousExls = absoluteExls(bui);
      var occupied = buildOccupied(state, appId);
      var requestedCenter = {
        x: previousCenter.x + step.x,
        y: previousCenter.y + step.y
      };

      if (!inBounds(requestedCenter)) {
        return {
          moved: false,
          reason: "center-out-of-bounds",
          attemptedCenter: requestedCenter
        };
      }

      if (occupied[keyFor(requestedCenter)]) {
        return { moved: false, reason: "center-blocked" };
      }

      var finalExlPoints = previousExls.map(function (exl) {
        return {
          x: exl.x + step.x,
          y: exl.y + step.y
        };
      });

      if (!validExlPoints(requestedCenter, finalExlPoints, occupied)) {
        return {
          moved: false,
          reason: classifyInvalidExlPoints(requestedCenter, finalExlPoints, occupied) || "exl-blocked",
          attemptedCenter: requestedCenter,
          attemptedExls: finalExlPoints
        };
      }

      finalExlPoints = applyExlLag(previousExls, finalExlPoints, requestedCenter, occupied);

      var nextExls = finalExlPoints.map(function (point) {
        return {
          dx: point.x - requestedCenter.x,
          dy: point.y - requestedCenter.y
        };
      });

      if (!isValidBui({ center: requestedCenter, exls: nextExls })) {
        return { moved: false, reason: "body-disconnected" };
      }

      bui.center = requestedCenter;
      bui.exls = nextExls;
      collectPills(state, appId);
      return { moved: keyFor(previousCenter) !== keyFor(requestedCenter), reason: null };
    }

    function recordExlMoveDebug(appId, requestedMoves, result, details) {
      appendDebug({
        tick: state.tick,
        appId: appId,
        operation: "moveExls",
        requestedMoves: requestedMoves,
        result: result,
        details: details || null
      });
    }

    function normalizeExlDirection(move) {
      var source = move && move.direction ? move.direction : move;
      var x = Number(source && (source.x == null ? source.dx : source.x));
      var y = Number(source && (source.y == null ? source.dy : source.y));

      if (!Number.isFinite(x) || !Number.isFinite(y)) {
        return null;
      }

      if (x < -1 || x > 1 || y < -1 || y > 1 || Math.floor(x) !== x || Math.floor(y) !== y) {
        return null;
      }

      if (x === 0 && y === 0) {
        return null;
      }

      return { x: x, y: y };
    }

    function normalizeExlIndex(move, exlCount) {
      var index = Number(move && (move.index == null ? move.exlIndex : move.index));
      if (!Number.isInteger(index) || index < 0 || index >= exlCount) {
        return null;
      }
      return index;
    }

    function moveExls(appId, moves) {
      var appState = state.apps[appId];
      var bui = appState.bui;
      var occupied = buildOccupied(state, appId);
      var finalExlPoints = absoluteExls(bui).map(function (exl) {
        return { x: exl.x, y: exl.y };
      });
      var seenIndexes = Object.create(null);
      var nextExls;

      if (exlMoveApplied[appId]) {
        var alreadyMoved = { applied: false, reason: "exls-already-moved" };
        recordExlMoveDebug(appId, moves, alreadyMoved);
        return alreadyMoved;
      }

      exlMoveApplied[appId] = true;

      if (!Array.isArray(moves) || moves.length !== 2) {
        var wrongCount = { applied: false, reason: "two-exl-moves-required" };
        recordExlMoveDebug(appId, moves, wrongCount);
        return wrongCount;
      }

      for (var moveIndex = 0; moveIndex < moves.length; moveIndex += 1) {
        var move = moves[moveIndex];
        var exlIndex = normalizeExlIndex(move, finalExlPoints.length);
        var direction = normalizeExlDirection(move);

        if (exlIndex == null || !direction || seenIndexes[exlIndex]) {
          var invalidMove = { applied: false, reason: "invalid-exl-move" };
          recordExlMoveDebug(appId, moves, invalidMove, { moveIndex: moveIndex });
          return invalidMove;
        }

        seenIndexes[exlIndex] = true;
        finalExlPoints[exlIndex] = {
          x: finalExlPoints[exlIndex].x + direction.x,
          y: finalExlPoints[exlIndex].y + direction.y
        };
      }

      if (finalExlPoints.some(function (point) {
        return !inBounds(point) || occupied[keyFor(point)];
      })) {
        var blocked = { applied: false, reason: "exl-target-blocked" };
        recordExlMoveDebug(appId, moves, blocked);
        return blocked;
      }

      nextExls = finalExlPoints.map(function (point) {
        return {
          dx: point.x - bui.center.x,
          dy: point.y - bui.center.y
        };
      });

      if (!isValidBui({ center: bui.center, exls: nextExls })) {
        var disconnected = { applied: false, reason: "body-disconnected" };
        recordExlMoveDebug(appId, moves, disconnected);
        return disconnected;
      }

      bui.exls = nextExls;
      collectPills(state, appId);

      var applied = { applied: true };
      recordExlMoveDebug(appId, moves, applied);
      return applied;
    }

    function handleAddPills() {
      addPills(state, 50);
      persistAndRender();
    }

    function handleResetWorld() {
      resetWorld();
    }

    if (options.addPillsButton) {
      options.addPillsButton.addEventListener("click", handleAddPills);
    }

    if (options.resetButton) {
      options.resetButton.addEventListener("click", handleResetWorld);
    }

    if (options.turboButton) {
      options.turboButton.addEventListener("click", function () {
        setTurboMode(!turboMode);
        if (running) {
          scheduleNextTick();
        }
      });
      updateTurboButton();
    }

    function applyPower(appId, vector) {
      var appState = state.apps[appId];
      var requested = capVector(vector);
      var requestedMagnitude = vectorMagnitude(requested);
      var momentumBefore = {
        x: appState.momentum.x,
        y: appState.momentum.y
      };
      var cost;
      var result;
      var scale;

      if (powerApplied[appId] || requestedMagnitude <= 0 || appState.power <= 0) {
        result = { applied: false, reason: powerApplied[appId] ? "power-already-applied" : "no-power" };
        appendDebug({
          tick: state.tick,
          appId: appId,
          operation: "applyPower",
          requestedVector: vector,
          appliedVector: requested,
          result: result,
          momentumBefore: momentumBefore,
          momentumAfter: momentumBefore
        });
        return result;
      }

      normalizeAppRuntimeState(appState);
      cost = requestedMagnitude * THRUST_POWER_COST;
      if (cost <= 0) {
        result = { applied: false, reason: "zero-thrust" };
        appendDebug({
          tick: state.tick,
          appId: appId,
          operation: "applyPower",
          requestedVector: vector,
          appliedVector: requested,
          result: result,
          momentumBefore: momentumBefore,
          momentumAfter: momentumBefore
        });
        return result;
      }

      scale = Math.min(1, appState.power / cost);
      appState.power -= cost * scale;
      if (appState.power <= 0) {
        appState.power = 0;
      }
      appState.momentum = capVector({
        x: appState.momentum.x + (requested.x * scale * POWER_VECTOR_ACCELERATION),
        y: appState.momentum.y + (requested.y * scale * POWER_VECTOR_ACCELERATION)
      });
      powerApplied[appId] = true;

      result = {
        applied: true,
        power: appState.power,
        momentum: { x: appState.momentum.x, y: appState.momentum.y }
      };
      appendDebug({
        tick: state.tick,
        appId: appId,
        operation: "applyPower",
        requestedVector: vector,
        appliedVector: requested,
        result: result,
        momentumBefore: momentumBefore,
        momentumAfter: {
          x: appState.momentum.x,
          y: appState.momentum.y
        }
      });
      return result;
    }

    function squaredDistance(first, second) {
      var dx = first.x - second.x;
      var dy = first.y - second.y;
      return (dx * dx) + (dy * dy);
    }

    function visiblePixels(appId, center, radius) {
      var maxDistance = radius * radius;
      var pixels = [];

      state.pills.forEach(function (pill) {
        if (squaredDistance(center, pill) <= maxDistance) {
          pixels.push({
            x: pill.x,
            y: pill.y,
            kind: "pill",
            owner: "world"
          });
        }
      });

      Object.keys(state.apps).forEach(function (ownerId) {
        var bui = state.apps[ownerId].bui;
        var centerPixel = {
          x: bui.center.x,
          y: bui.center.y,
          kind: "center",
          owner: ownerId
        };

        if (squaredDistance(center, centerPixel) <= maxDistance) {
          pixels.push(centerPixel);
        }

        absoluteExls(bui).forEach(function (exl) {
          if (squaredDistance(center, exl) <= maxDistance) {
            pixels.push({
              x: exl.x,
              y: exl.y,
              kind: "exl",
              owner: ownerId
            });
          }
        });
      });

      return pixels;
    }

    function look(appId) {
      var appState = state.apps[appId];
      var center = {
        x: appState.bui.center.x,
        y: appState.bui.center.y
      };
      var result;
      var pixels;

      if (lookApplied[appId]) {
        result = { applied: false, reason: "look-already-applied" };
        appendDebug({
          tick: state.tick,
          appId: appId,
          operation: "look",
          result: result
        });
        return result;
      }

      lookApplied[appId] = true;

      if (appState.power < LOOK_POWER_COST) {
        result = { applied: false, reason: "insufficient-power", cost: LOOK_POWER_COST };
        appendDebug({
          tick: state.tick,
          appId: appId,
          operation: "look",
          result: result
        });
        return result;
      }

      appState.power -= LOOK_POWER_COST;
      pixels = visiblePixels(appId, center, LOOK_RADIUS);
      renderer.showLook(center, LOOK_RADIUS, colorsByApp[appId]);

      result = {
        applied: true,
        cost: LOOK_POWER_COST,
        radius: LOOK_RADIUS,
        center: center,
        power: appState.power,
        pixels: pixels,
        pills: pixels.filter(function (pixel) {
          return pixel.kind === "pill";
        }).map(function (pill) {
          return { x: pill.x, y: pill.y };
        }),
        creatures: pixels.filter(function (pixel) {
          return pixel.kind === "center" && pixel.owner !== appId;
        }).map(function (pixel) {
          return { appId: pixel.owner, x: pixel.x, y: pixel.y };
        })
      };
      appendDebug({
        tick: state.tick,
        appId: appId,
        operation: "look",
        result: {
          applied: true,
          cost: LOOK_POWER_COST,
          radius: LOOK_RADIUS,
          pixels: pixels.length,
          pills: result.pills.length,
          powerAfter: appState.power
        }
      });
      return result;
    }

    function consumeMotion(motion, axis) {
      if (motion[axis] >= 1) {
        motion[axis] -= 1;
        return 1;
      }

      if (motion[axis] <= -1) {
        motion[axis] += 1;
        return -1;
      }

      return 0;
    }

    function advanceBui(appId) {
      var appState = state.apps[appId];
      var step;
      var result;
      var previousMomentum;

      normalizeAppRuntimeState(appState);
      if (vectorMagnitude(appState.momentum) < 0.01) {
        return;
      }

      if (!appState.motion) {
        appState.motion = createMotion();
      }

      appState.motion.carryX += appState.momentum.x;
      appState.motion.carryY += appState.momentum.y;
      step = {
        x: consumeMotion(appState.motion, "carryX"),
        y: consumeMotion(appState.motion, "carryY")
      };

      if (step.x === 0 && step.y === 0) {
        return;
      }

      previousMomentum = {
        x: appState.momentum.x,
        y: appState.momentum.y
      };
      result = moveStep(appId, step);
      if (!result.moved) {
        if (result.reason === "center-out-of-bounds" || result.reason === "body-out-of-bounds") {
          reflectMomentumFromBounds(appState, step, result.attemptedCenter, result.attemptedExls);
        } else {
          appState.momentum = randomFullSpeed();
          appState.motion = createMotion();
        }
        appendDebug({
          tick: state.tick,
          appId: appId,
          operation: "advanceBui",
          step: step,
          result: result,
          momentumBefore: previousMomentum,
          momentumAfter: {
            x: appState.momentum.x,
            y: appState.momentum.y
          }
        });
      } else {
        appState.momentum = capVector({
          x: appState.momentum.x * MOMENTUM_DECAY,
          y: appState.momentum.y * MOMENTUM_DECAY
        });
        appState.moveCostMoves += 1;
        if (appState.moveCostMoves >= MOVE_COST_INTERVAL) {
          var penalty = Math.floor(appState.moveCostMoves / MOVE_COST_INTERVAL);
          appState.moveCostMoves %= MOVE_COST_INTERVAL;
          appState.power = Math.max(0, appState.power - penalty);
          appState.lastPowerDelta -= penalty;
          appendDebug({
            tick: state.tick,
            appId: appId,
            operation: "movementPenalty",
            penalty: penalty,
            powerAfter: appState.power,
            moveCostMoves: appState.moveCostMoves
          });
        }
      }
    }

    function createApi(appId, feedback) {
      var appStore = createAppStateStore(appId);
      return {
        getBui: function () {
          var bui = state.apps[appId].bui;
          var lastPowerDelta = feedback ? feedback.lastPowerDelta : state.apps[appId].lastPowerDelta;
          var lastPillsCollected = feedback ? feedback.lastPillsCollected : state.apps[appId].lastPillsCollected;
          return {
            center: { x: bui.center.x, y: bui.center.y },
            exls: bui.exls.map(function (exl) {
              return { dx: exl.dx, dy: exl.dy };
            }),
            momentum: {
              x: state.apps[appId].momentum.x,
              y: state.apps[appId].momentum.y
            },
            power: state.apps[appId].power,
            moveCostMoves: state.apps[appId].moveCostMoves,
            coastMoves: state.apps[appId].moveCostMoves,
            lastPowerDelta: lastPowerDelta,
            lastPillsCollected: lastPillsCollected
          };
        },
        getBody: function () {
          return bodyPoints(state.apps[appId].bui).map(function (point) {
            return { x: point.x, y: point.y };
          });
        },
        applyPower: function (vector) {
          return applyPower(appId, vector);
        },
        moveExls: function (moves) {
          return moveExls(appId, moves);
        },
        look: function () {
          return look(appId);
        },
        loadState: appStore.load,
        saveState: appStore.save
      };
    }

    function tick() {
      state.tick += 1;
      powerApplied = Object.create(null);
      exlMoveApplied = Object.create(null);
      lookApplied = Object.create(null);
      deactivateUnpowered();

      apps.forEach(function (app) {
        var appRuntime = registeredApps[app.id];
        var feedback = {
          lastPowerDelta: state.apps[app.id].lastPowerDelta,
          lastPillsCollected: state.apps[app.id].lastPillsCollected
        };
        state.apps[app.id].lastPowerDelta = 0;
        state.apps[app.id].lastPillsCollected = 0;
        if (state.apps[app.id].active && appRuntime && typeof appRuntime.tick === "function") {
          appRuntime.tick(createApi(app.id, feedback));
        }
        if (state.apps[app.id].active) {
          advanceBui(app.id);
        }
        deactivateUnpowered();
        saveJson(WORLD_STATE_KEY, state);
      });

      if (turboMode && poweredBuiCount() <= 1) {
        setTurboMode(false);
      }
      persistAndRender();
      scheduleNextTick();
    }

    function scheduleNextTick() {
      if (!running) {
        return;
      }
      if (tickTimer != null) {
        window.clearTimeout(tickTimer);
      }
      tickTimer = window.setTimeout(tick, turboMode ? 0 : TICK_MS);
    }

    function start() {
      running = true;
      renderControls();
      persistAndRender();
      scheduleNextTick();
    }

    return { start: start };
  }

  window.WebPixelWorld = {
    create: createWorld,
    getDebugLog: function () {
      return loadJson(DEBUG_LOG_KEY, []);
    },
    clearDebugLog: function () {
      saveJson(DEBUG_LOG_KEY, []);
    },
    registerApp: function (app) {
      registeredApps[app.id] = app;
    }
  };
})();
