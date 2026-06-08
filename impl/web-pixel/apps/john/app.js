(function () {
  var WORLD_WIDTH = 200;
  var WORLD_HEIGHT = 150;
  var EDGE_MARGIN = 12;
  var LANE_HEIGHT = 12;
  var TURN_DISTANCE = 5;
  var LOOK_POWER_COST = 4;
  var LOOK_COOLDOWN_TICKS = 30;
  var TARGET_MAX_AGE = 55;
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
      return { x: 0, y: 0 };
    }
    return { x: vector.x / length, y: vector.y / length };
  }

  function clamp(value, min, max) {
    return Math.max(min, Math.min(max, value));
  }

  function sign(value) {
    if (value > 0) {
      return 1;
    }
    if (value < 0) {
      return -1;
    }
    return 0;
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

  function uniquePills(pills) {
    var seen = Object.create(null);
    var result = [];

    (pills || []).forEach(function (pill) {
      var point = {
        x: Number(pill && pill.x),
        y: Number(pill && pill.y)
      };
      var key = point.x + "," + point.y;

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

  function removeCollectedPills(pills, bui, target) {
    var collected = Object.create(null);

    absoluteExls(bui).forEach(function (exl) {
      collected[keyFor(exl)] = true;
    });

    return removePillsNear(pills, target, 4).filter(function (pill) {
      return !collected[keyFor(pill)];
    });
  }

  function prunePillsNearBody(pills, bui, radiusSq) {
    var bodyPoints = [bui.center].concat(absoluteExls(bui));
    return uniquePills(pills).filter(function (pill) {
      for (var i = 0; i < bodyPoints.length; i += 1) {
        if (distanceSq(pill, bodyPoints[i]) <= radiusSq) {
          return false;
        }
      }
      return true;
    });
  }

  var CLUSTER_RADIUS_SQ = 144;
  var CLUSTER_BONUS = 60;
  var RIVAL_SWEEP_RADIUS_SQ = 64;
  var RIVAL_SWEEP_PENALTY = 1500;
  var RIVAL_LOOKAHEAD_STEPS = 3;

  function pillsNear(pills, point, radiusSq) {
    var count = 0;
    pills.forEach(function (pill) {
      if (distanceSq(pill, point) <= radiusSq) {
        count += 1;
      }
    });
    return count;
  }

  function rivalSweepPenalty(pill, rivals) {
    var penalty = 0;
    (rivals || []).forEach(function (rival) {
      var heading = rival.heading || { x: 0, y: 0 };
      for (var step = 0; step <= RIVAL_LOOKAHEAD_STEPS; step += 1) {
        var probe = {
          x: rival.x + (heading.x * step * 4),
          y: rival.y + (heading.y * step * 4)
        };
        if (distanceSq(pill, probe) <= RIVAL_SWEEP_RADIUS_SQ) {
          penalty += RIVAL_SWEEP_PENALTY;
          return;
        }
      }
    });
    return penalty;
  }

  function choosePill(pills, center, momentum, rivals) {
    var heading = magnitude(momentum) > 0.05 ? normalize(momentum) : null;
    var unique = uniquePills(pills);
    var best = null;

    unique.forEach(function (pill) {
      var relative = {
        x: pill.x - center.x,
        y: pill.y - center.y
      };
      var forward = heading ? dot(normalize(relative), heading) : 1;
      var density = pillsNear(unique, pill, CLUSTER_RADIUS_SQ);
      var score = distanceSq(center, pill)
        + (forward < 0 ? 900 : 0)
        - (density * CLUSTER_BONUS)
        + rivalSweepPenalty(pill, rivals);

      if (!best || score < best.score) {
        best = {
          x: pill.x,
          y: pill.y,
          score: score
        };
      }
    });

    return best ? { x: best.x, y: best.y } : null;
  }

  function promoteKnownTarget(state, bui) {
    state.knownPills = uniquePills(state.knownPills);
    state.target = choosePill(state.knownPills, bui.center, bui.momentum, rivalForecasts(state));
    state.targetAge = 0;
    return state.target;
  }

  function rivalForecasts(state) {
    var rivals = state.rivals || {};
    var forecasts = [];
    Object.keys(rivals).forEach(function (appId) {
      var entry = rivals[appId];
      if (!entry || !entry.last) {
        return;
      }
      forecasts.push({
        appId: appId,
        x: entry.last.x,
        y: entry.last.y,
        heading: entry.heading || { x: 0, y: 0 }
      });
    });
    return forecasts;
  }

  function updateRivals(state, sighted, tick) {
    if (!state.rivals) {
      state.rivals = {};
    }
    (sighted || []).forEach(function (creature) {
      var entry = state.rivals[creature.appId] || {};
      var prev = entry.last;
      var heading = { x: 0, y: 0 };
      if (prev && (creature.x !== prev.x || creature.y !== prev.y)) {
        var dt = Math.max(1, tick - (entry.tick || tick));
        heading = normalize({
          x: (creature.x - prev.x) / dt,
          y: (creature.y - prev.y) / dt
        });
      } else if (entry.heading) {
        heading = entry.heading;
      }
      state.rivals[creature.appId] = {
        last: { x: creature.x, y: creature.y },
        heading: heading,
        tick: tick
      };
    });
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

  function desiredOffsets(count) {
    var offsets = [];
    var distance = 1;

    while (offsets.length < count) {
      offsets.push(-distance);
      if (offsets.length < count) {
        offsets.push(distance);
      }
      distance += 1;
    }

    return offsets.sort(function (a, b) {
      return a - b;
    });
  }

  var ARM_ROTATION_STEP = Math.PI / 48;
  var ARM_TANGENTIAL_WEIGHT = 2;
  var ARM_CONVERGED_SCORE_FACTOR = 4;

  function armBodyScore(center, points, angle) {
    var dir = { x: Math.cos(angle), y: Math.sin(angle) };
    var projections = points.map(function (point) {
      var rel = { x: point.x - center.x, y: point.y - center.y };
      return {
        radial: (rel.x * dir.x) + (rel.y * dir.y),
        tangential: (rel.x * -dir.y) + (rel.y * dir.x)
      };
    }).sort(function (a, b) {
      return a.radial - b.radial;
    });
    var score = 0;

    projections.forEach(function (projection, index) {
      var expectedRadial = index + 1;
      var radialError = projection.radial - expectedRadial;
      var tangentialError = projection.tangential;
      score += (radialError * radialError) + (tangentialError * tangentialError * ARM_TANGENTIAL_WEIGHT);
    });

    return score;
  }

  function chooseArmBodyMove(bui, angle) {
    var points = absoluteExls(bui);
    var currentScore = armBodyScore(bui.center, points, angle);
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

            score = armBodyScore(bui.center, nextPoints, angle);
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

    if (!best || best.score >= currentScore) {
      return {
        skipped: true,
        reason: "no-arm-improvement",
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

  function rotateArm(world, state, bui) {
    var currentAngle = Number(state.armAngle) || 0;
    var currentScore = armBodyScore(bui.center, absoluteExls(bui), currentAngle);
    var convergedThreshold = bui.exls.length * ARM_CONVERGED_SCORE_FACTOR;

    if (currentScore < convergedThreshold) {
      currentAngle += ARM_ROTATION_STEP;
      if (currentAngle > Math.PI * 2) {
        currentAngle -= Math.PI * 2;
      }
      state.armAngle = currentAngle;
    }

    var plan = chooseArmBodyMove(bui, currentAngle);
    var result = null;

    if (plan && !plan.skipped && typeof world.moveExls === "function") {
      result = world.moveExls(plan.moves);
    }

    state.lastBodyMove = {
      plan: plan,
      result: result,
      armAngle: currentAngle,
      armScore: currentScore,
      converged: currentScore < convergedThreshold
    };
  }

  function captureWaypoint(bui, target) {
    var heading = magnitude(bui.momentum) > 0.05 ? normalize(bui.momentum) : normalize({
      x: target.x - bui.center.x,
      y: target.y - bui.center.y
    });

    return {
      x: clamp(target.x - (heading.x * 3), EDGE_MARGIN, WORLD_WIDTH - EDGE_MARGIN),
      y: clamp(target.y - (heading.y * 3), EDGE_MARGIN, WORLD_HEIGHT - EDGE_MARGIN)
    };
  }

  function chooseWaypoint(state, center) {
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

  function steeringPower(momentum, desired) {
    return {
      x: (desired.x - momentum.x) * 0.75,
      y: (desired.y - momentum.y) * 0.75
    };
  }

  function conservativeSteering(momentum, desired, hasTarget) {
    var speed = magnitude(momentum);
    var heading = speed > 0.05 ? normalize(momentum) : desired;
    var alignment = dot(heading, desired);
    var minimumSpeed = hasTarget ? 0.62 : 0.45;
    var alignmentLimit = hasTarget ? 0.995 : 0.98;

    if (alignment < alignmentLimit || speed < minimumSpeed) {
      return steeringPower(momentum, desired);
    }

    return null;
  }

  function maybeLook(world, state, bui) {
    var cooldown = Math.max(0, Number(state.lookCooldown) || 0);
    var result = null;
    var pillCount = (state.knownPills || []).length;
    var ticksSinceLook = (Number(state.tick) || 0) - (Number(state.lastLookTick) || 0);
    var needsLook = pillCount === 0 || (pillCount < 4 && ticksSinceLook > 50) || ticksSinceLook > 120;

    if (cooldown > 0) {
      state.lookCooldown = cooldown - 1;
      return null;
    }

    if (bui.power <= LOOK_POWER_COST + 8 || !needsLook || typeof world.look !== "function") {
      return null;
    }

    result = world.look();
    state.lookCooldown = LOOK_COOLDOWN_TICKS;
    if (result && result.applied) {
      state.knownPills = uniquePills(result.pills);
      state.lastLookTick = state.tick || 0;
      updateRivals(state, result.creatures, state.tick || 0);
    }

    return result;
  }

  var EDGE_BOUNCE = 6;
  var DRIFT_SPEED = 0.18;
  var SEARCH_SPEED = 0.42;
  var BIAS_THRUST_MAX = 0.04;
  var BIAS_ALIGNMENT_MIN = 0.85;

  function cruiseAlong(momentum, fallbackDirection, targetSpeed) {
    var speed = magnitude(momentum);
    if (speed < 0.01) {
      return {
        x: fallbackDirection.x * 0.08,
        y: fallbackDirection.y * 0.08
      };
    }
    if (speed >= targetSpeed) {
      return null;
    }
    var amount = Math.min(1, (targetSpeed - speed) * 0.18);
    return {
      x: (momentum.x / speed) * amount,
      y: (momentum.y / speed) * amount
    };
  }

  function blendedSteer(momentum, desiredDirection, targetSpeed) {
    var speed = magnitude(momentum);
    var heading = speed > 0.05 ? normalize(momentum) : desiredDirection;
    var alignment = dot(heading, desiredDirection);
    var cruise = cruiseAlong(momentum, heading, targetSpeed);

    if (alignment <= 0) {
      return cruise;
    }

    var side = { x: -heading.y, y: heading.x };
    var sideComponent = (desiredDirection.x * side.x) + (desiredDirection.y * side.y);
    var lateral = Math.max(-1, Math.min(1, sideComponent)) * BIAS_THRUST_MAX;
    var lateralThrust = { x: side.x * lateral, y: side.y * lateral };

    if (!cruise) {
      return lateralThrust;
    }
    return { x: cruise.x + lateralThrust.x, y: cruise.y + lateralThrust.y };
  }

  function edgeAvoidance(center, momentum) {
    var pushX = 0;
    var pushY = 0;
    if (center.x < EDGE_BOUNCE) { pushX = 1; }
    else if (center.x > WORLD_WIDTH - EDGE_BOUNCE) { pushX = -1; }
    if (center.y < EDGE_BOUNCE) { pushY = 1; }
    else if (center.y > WORLD_HEIGHT - EDGE_BOUNCE) { pushY = -1; }
    if (pushX === 0 && pushY === 0) {
      return null;
    }
    var desired = normalize({ x: pushX, y: pushY });
    var heading = magnitude(momentum) > 0.05 ? normalize(momentum) : desired;
    var alignment = dot(heading, desired);
    if (alignment > 0.5) {
      return null;
    }
    return {
      x: desired.x * 0.1,
      y: desired.y * 0.1
    };
  }

  function clusterCenter(pills) {
    if (!pills || pills.length === 0) {
      return null;
    }
    var sumX = 0;
    var sumY = 0;
    pills.forEach(function (pill) {
      sumX += pill.x;
      sumY += pill.y;
    });
    return { x: sumX / pills.length, y: sumY / pills.length };
  }

  window.WebPixelWorld.registerApp({
    id: "john",
    tick: function (world) {
      var state = world.loadState();
      var bui = world.getBui();
      var lookResult;

      state.tick = (Number(state.tick) || 0) + 1;
      state.knownPills = prunePillsNearBody(state.knownPills, bui, 16);
      if (Number(bui.lastPillsCollected) > 0) {
        state.knownPills = removeCollectedPills(state.knownPills, bui, null);
      }

      lookResult = maybeLook(world, state, bui);

      var heading = magnitude(bui.momentum) > 0.05 ? normalize(bui.momentum) : { x: 1, y: 0 };
      rotateArm(world, state, bui);

      var edge = edgeAvoidance(bui.center, bui.momentum);
      var desiredDirection = heading;
      var biasDesired = null;
      var hasKnownPills = state.knownPills && state.knownPills.length > 0;

      if (hasKnownPills) {
        var cluster = clusterCenter(state.knownPills);
        if (cluster) {
          biasDesired = normalize({
            x: cluster.x - bui.center.x,
            y: cluster.y - bui.center.y
          });
          desiredDirection = biasDesired;
        }
      }

      var targetSpeed = hasKnownPills ? DRIFT_SPEED : SEARCH_SPEED;
      var thrust = edge || blendedSteer(bui.momentum, desiredDirection, targetSpeed);
      var result = null;
      if (bui.power > 0 && thrust) {
        result = world.applyPower(thrust);
      }

      state.lastSteering = {
        heading: heading,
        knownPillCount: state.knownPills.length,
        biasDesired: biasDesired,
        look: lookResult,
        lastPillsCollected: bui.lastPillsCollected,
        lastPowerDelta: bui.lastPowerDelta,
        thrust: thrust,
        edgeActive: Boolean(edge),
        biasActive: Boolean(biasDesired),
        result: result,
        moveCostMoves: bui.moveCostMoves
      };
      world.saveState(state);
    }
  });
})();
