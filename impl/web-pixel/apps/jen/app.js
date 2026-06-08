(function () {
  var WORLD_WIDTH = 200;
  var WORLD_HEIGHT = 150;
  var EDGE_MARGIN = 12;
  var LANE_HEIGHT = 12;
  var TURN_DISTANCE = 5;
  var LOOK_POWER_COST = 4;
  var LOOK_COOLDOWN_TICKS = 26;
  var TARGET_MAX_AGE = 70;
  var TARGET_COLLECTED_RADIUS_SQ = 0;
  var SEARCH_SPEED = 0.52;
  var HUNT_SPEED = 0.72;
  var DIRECTIONS = [
    { x: -1, y: -1 },
    { x: 0, y: -1 },
    { x: 1, y: -1 },
    { x: -1, y: 0 },
    { x: 1, y: 0 },
    { x: -1, y: 1 },
    { x: 0, y: 1 },
    { x: 1, y: 1 }
  ];

  function magnitude(vector) {
    return Math.sqrt((vector.x * vector.x) + (vector.y * vector.y));
  }

  function dot(first, second) {
    return (first.x * second.x) + (first.y * second.y);
  }

  function normalize(vector) {
    var length = magnitude(vector);
    if (length === 0) {
      return { x: 1, y: 0 };
    }
    return { x: vector.x / length, y: vector.y / length };
  }

  function clamp(value, min, max) {
    return Math.max(min, Math.min(max, value));
  }

  function keyFor(point) {
    return point.x + "," + point.y;
  }

  function inBounds(point) {
    return point.x >= 0 && point.y >= 0 && point.x < WORLD_WIDTH && point.y < WORLD_HEIGHT;
  }

  function distanceSq(first, second) {
    var dx = first.x - second.x;
    var dy = first.y - second.y;
    return (dx * dx) + (dy * dy);
  }

  function absoluteExls(bui) {
    return bui.exls.map(function (exl, index) {
      return {
        index: index,
        x: bui.center.x + exl.dx,
        y: bui.center.y + exl.dy
      };
    });
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

  function isConnected(center, points) {
    var pointSet = Object.create(null);
    var seen = Object.create(null);
    var queue = [center];

    pointSet[keyFor(center)] = true;
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

  function locallyValid(center, points) {
    var taken = Object.create(null);
    taken[keyFor(center)] = true;

    return points.every(function (point) {
      var key = keyFor(point);
      if (!inBounds(point) || taken[key]) {
        return false;
      }
      taken[key] = true;
      return true;
    }) && isConnected(center, points);
  }

  function uniquePills(pills) {
    var seen = Object.create(null);
    var result = [];

    (pills || []).forEach(function (pill) {
      var point = {
        x: Number(pill && pill.x),
        y: Number(pill && pill.y)
      };
      var key = keyFor(point);

      if (Number.isFinite(point.x) && Number.isFinite(point.y) && !seen[key]) {
        seen[key] = true;
        result.push(point);
      }
    });

    return result;
  }

  function removePillsNear(pills, point, radiusSq) {
    if (!point) {
      return uniquePills(pills);
    }

    return uniquePills(pills).filter(function (pill) {
      return distanceSq(pill, point) > radiusSq;
    });
  }

  function prunePillsNearExls(pills, bui, radiusSq) {
    var bodyPoints = absoluteExls(bui);

    return uniquePills(pills).filter(function (pill) {
      for (var index = 0; index < bodyPoints.length; index += 1) {
        if (distanceSq(pill, bodyPoints[index]) <= radiusSq) {
          return false;
        }
      }
      return true;
    });
  }

  function nearestExlDistanceSq(bui, target) {
    var nearest = Infinity;

    absoluteExls(bui).forEach(function (exl) {
      nearest = Math.min(nearest, distanceSq(exl, target));
    });

    return nearest;
  }

  function choosePill(pills, center, momentum) {
    var heading = magnitude(momentum) > 0.05 ? normalize(momentum) : null;
    var best = null;

    uniquePills(pills).forEach(function (pill) {
      var relative = {
        x: pill.x - center.x,
        y: pill.y - center.y
      };
      var forward = heading ? dot(normalize(relative), heading) : 1;
      var score = distanceSq(center, pill) + (forward < -0.15 ? 650 : 0);

      if (!best || score < best.score) {
        best = { x: pill.x, y: pill.y, score: score };
      }
    });

    return best ? { x: best.x, y: best.y } : null;
  }

  function promoteTarget(state, bui) {
    state.knownPills = uniquePills(state.knownPills);
    state.target = choosePill(state.knownPills, bui.center, bui.momentum);
    state.targetAge = 0;
    return state.target;
  }

  function maybeLook(world, state, bui) {
    var cooldown = Math.max(0, Number(state.lookCooldown) || 0);
    var ticksSinceLook = (Number(state.tick) || 0) - (Number(state.lastLookTick) || 0);
    var knownCount = (state.knownPills || []).length;
    var needsLook = knownCount === 0 || (knownCount < 3 && ticksSinceLook > 45) || ticksSinceLook > 125;
    var result = null;

    if (cooldown > 0) {
      state.lookCooldown = cooldown - 1;
      return null;
    }

    if (!needsLook || bui.power <= LOOK_POWER_COST + 8 || typeof world.look !== "function") {
      return null;
    }

    result = world.look();
    state.lookCooldown = LOOK_COOLDOWN_TICKS;
    if (result && result.applied) {
      state.knownPills = uniquePills((state.knownPills || []).concat(result.pills || []));
      state.lastLookTick = state.tick || 0;
    }

    return result;
  }

  function movedPoints(points, firstIndex, firstDirection, secondIndex, secondDirection) {
    return points.map(function (point) {
      if (point.index === firstIndex) {
        return {
          index: point.index,
          x: point.x + firstDirection.x,
          y: point.y + firstDirection.y
        };
      }
      if (point.index === secondIndex) {
        return {
          index: point.index,
          x: point.x + secondDirection.x,
          y: point.y + secondDirection.y
        };
      }
      return {
        index: point.index,
        x: point.x,
        y: point.y
      };
    });
  }

  function grabScore(center, points, target) {
    var heading = normalize({
      x: target.x - center.x,
      y: target.y - center.y
    });
    var side = { x: -heading.y, y: heading.x };
    var nearest = Infinity;
    var forwardTip = -Infinity;
    var sideTotal = 0;

    points.forEach(function (point) {
      var relative = {
        x: point.x - center.x,
        y: point.y - center.y
      };
      nearest = Math.min(nearest, distanceSq(point, target));
      forwardTip = Math.max(forwardTip, dot(relative, heading));
      sideTotal += Math.abs(dot(relative, side));
    });

    return nearest - (forwardTip * 2.5) + (sideTotal * 0.08);
  }

  function chooseGrabMove(bui, target) {
    var points = absoluteExls(bui);
    var currentScore = grabScore(bui.center, points, target);
    var best = null;

    if (points.length < 2) {
      return null;
    }

    for (var first = 0; first < points.length - 1; first += 1) {
      for (var second = first + 1; second < points.length; second += 1) {
        DIRECTIONS.forEach(function (firstDirection) {
          DIRECTIONS.forEach(function (secondDirection) {
            var nextPoints = movedPoints(points, points[first].index, firstDirection, points[second].index, secondDirection);
            var score;

            if (!locallyValid(bui.center, nextPoints)) {
              return;
            }

            score = grabScore(bui.center, nextPoints, target);
            if (!best || score < best.score) {
              best = {
                score: score,
                moves: [
                  { index: points[first].index, direction: firstDirection },
                  { index: points[second].index, direction: secondDirection }
                ]
              };
            }
          });
        });
      }
    }

    if (!best || best.score >= currentScore - 0.15) {
      return {
        skipped: true,
        reason: "no-grab-improvement",
        score: currentScore
      };
    }

    return {
      skipped: false,
      scoreBefore: currentScore,
      scoreAfter: best.score,
      moves: best.moves
    };
  }

  function moveBodyTowardTarget(world, state, bui, target) {
    var plan = chooseGrabMove(bui, target);
    var result = null;

    if (plan && !plan.skipped && typeof world.moveExls === "function") {
      result = world.moveExls(plan.moves);
    }

    state.lastBodyMove = {
      plan: plan,
      result: result,
      target: target,
      center: bui.center
    };
  }

  function chooseSweepWaypoint(state, center) {
    if (!state.mode) {
      state.mode = "east";
      state.laneY = clamp(center.y, EDGE_MARGIN, WORLD_HEIGHT - EDGE_MARGIN);
    }

    if (state.mode === "east" && center.x >= WORLD_WIDTH - EDGE_MARGIN) {
      state.mode = "drop-west";
      state.laneY = clamp(state.laneY + LANE_HEIGHT, EDGE_MARGIN, WORLD_HEIGHT - EDGE_MARGIN);
    } else if (state.mode === "west" && center.x <= EDGE_MARGIN) {
      state.mode = "drop-east";
      state.laneY = clamp(state.laneY + LANE_HEIGHT, EDGE_MARGIN, WORLD_HEIGHT - EDGE_MARGIN);
    }

    if ((state.mode === "drop-west" || state.mode === "drop-east") && Math.abs(center.y - state.laneY) <= TURN_DISTANCE) {
      state.mode = state.mode === "drop-west" ? "west" : "east";
    }

    if (state.laneY >= WORLD_HEIGHT - EDGE_MARGIN && center.y >= WORLD_HEIGHT - EDGE_MARGIN - TURN_DISTANCE) {
      state.mode = center.x < WORLD_WIDTH / 2 ? "east" : "west";
      state.laneY = EDGE_MARGIN;
    }

    if (state.mode === "east") {
      return { x: WORLD_WIDTH - EDGE_MARGIN, y: state.laneY };
    }
    if (state.mode === "west") {
      return { x: EDGE_MARGIN, y: state.laneY };
    }
    return {
      x: state.mode === "drop-west" ? WORLD_WIDTH - EDGE_MARGIN : EDGE_MARGIN,
      y: state.laneY
    };
  }

  function targetWaypoint(center, momentum, target) {
    var approach = normalize({
      x: target.x - center.x,
      y: target.y - center.y
    });
    var heading = magnitude(momentum) > 0.05 ? normalize(momentum) : approach;
    var distance = Math.sqrt(distanceSq(center, target));
    var overshoot = distance < 18 ? 7 : 2;

    return {
      x: clamp(target.x + (heading.x * overshoot), EDGE_MARGIN, WORLD_WIDTH - EDGE_MARGIN),
      y: clamp(target.y + (heading.y * overshoot), EDGE_MARGIN, WORLD_HEIGHT - EDGE_MARGIN)
    };
  }

  function steerToward(momentum, desiredDirection, targetSpeed, hasTarget) {
    var speed = magnitude(momentum);
    var desiredMomentum = {
      x: desiredDirection.x * targetSpeed,
      y: desiredDirection.y * targetSpeed
    };
    var heading = speed > 0.05 ? normalize(momentum) : desiredDirection;
    var alignment = dot(heading, desiredDirection);
    var alignmentLimit = hasTarget ? 0.992 : 0.975;

    if (alignment >= alignmentLimit && speed >= targetSpeed * 0.82) {
      return null;
    }

    return {
      x: (desiredMomentum.x - momentum.x) * 0.72,
      y: (desiredMomentum.y - momentum.y) * 0.72
    };
  }

  function avoidEdge(center, momentum) {
    var pushX = 0;
    var pushY = 0;

    if (center.x < 6) {
      pushX = 1;
    } else if (center.x > WORLD_WIDTH - 6) {
      pushX = -1;
    }
    if (center.y < 6) {
      pushY = 1;
    } else if (center.y > WORLD_HEIGHT - 6) {
      pushY = -1;
    }

    if (pushX === 0 && pushY === 0) {
      return null;
    }

    return steerToward(momentum, normalize({ x: pushX, y: pushY }), 0.58, false);
  }

  window.WebPixelWorld.registerApp({
    id: "jen",
    tick: function (world) {
      var state = world.loadState();
      var bui = world.getBui();
      var lookResult;
      var target;
      var waypoint;
      var desiredDirection;
      var thrust;
      var powerResult = null;
      var hasTarget;

      state.tick = (Number(state.tick) || 0) + 1;
      state.targetAge = (Number(state.targetAge) || 0) + 1;
      state.knownPills = prunePillsNearExls(state.knownPills, bui, TARGET_COLLECTED_RADIUS_SQ);

      if (Number(bui.lastPillsCollected) > 0) {
        state.knownPills = removePillsNear(state.knownPills, state.target, 16);
        state.target = null;
      }

      if (state.target && (state.targetAge > TARGET_MAX_AGE || nearestExlDistanceSq(bui, state.target) <= TARGET_COLLECTED_RADIUS_SQ)) {
        state.knownPills = removePillsNear(state.knownPills, state.target, 16);
        state.target = null;
      }

      lookResult = maybeLook(world, state, bui);

      if (!state.target) {
        promoteTarget(state, bui);
      }

      target = state.target;
      hasTarget = Boolean(target);
      if (hasTarget) {
        moveBodyTowardTarget(world, state, bui, target);
        waypoint = targetWaypoint(bui.center, bui.momentum, target);
      } else {
        waypoint = chooseSweepWaypoint(state, bui.center);
        state.lastBodyMove = {
          plan: null,
          result: null,
          target: null,
          center: bui.center
        };
      }

      desiredDirection = normalize({
        x: waypoint.x - bui.center.x,
        y: waypoint.y - bui.center.y
      });
      thrust = avoidEdge(bui.center, bui.momentum) || steerToward(bui.momentum, desiredDirection, hasTarget ? HUNT_SPEED : SEARCH_SPEED, hasTarget);

      if (bui.power > 0 && thrust && typeof world.applyPower === "function") {
        powerResult = world.applyPower(thrust);
      }

      state.lastLook = lookResult;
      state.lastSteering = {
        target: target,
        waypoint: waypoint,
        desiredDirection: desiredDirection,
        thrust: thrust,
        result: powerResult,
        knownPillCount: (state.knownPills || []).length,
        lastPillsCollected: bui.lastPillsCollected,
        lastPowerDelta: bui.lastPowerDelta,
        moveCostMoves: bui.moveCostMoves
      };

      world.saveState(state);
    }
  });
})();
