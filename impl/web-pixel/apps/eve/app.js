(function () {
  var WORLD_WIDTH = 200;
  var WORLD_HEIGHT = 150;
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

  function normalize(vector) {
    var length = magnitude(vector);
    if (length === 0) {
      return { x: 1, y: 0 };
    }
    return {
      x: vector.x / length,
      y: vector.y / length
    };
  }

  function keyFor(point) {
    return point.x + "," + point.y;
  }

  function inBounds(point) {
    return point.x >= 0 && point.y >= 0 && point.x < WORLD_WIDTH && point.y < WORLD_HEIGHT;
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

  function absoluteExls(bui) {
    return bui.exls.map(function (exl, index) {
      return {
        index: index,
        x: bui.center.x + exl.dx,
        y: bui.center.y + exl.dy
      };
    });
  }

  function locallyValid(center, points) {
    var taken = Object.create(null);
    taken[keyFor(center)] = true;

    for (var index = 0; index < points.length; index += 1) {
      var point = points[index];
      var key = keyFor(point);
      if (!inBounds(point) || taken[key]) {
        return false;
      }
      taken[key] = true;
    }

    return isConnected(center, points);
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

  function bodyScore(center, points, heading, phase) {
    var side = { x: -heading.y, y: heading.x };
    var offsets = desiredOffsets(points.length);
    var wave = Math.sin(phase * 0.17);
    var projections = points.map(function (point) {
      var relative = {
        x: point.x - center.x,
        y: point.y - center.y
      };
      return {
        forward: (relative.x * heading.x) + (relative.y * heading.y),
        side: (relative.x * side.x) + (relative.y * side.y)
      };
    }).sort(function (a, b) {
      return a.side - b.side;
    });
    var score = 0;

    projections.forEach(function (projection, index) {
      var offsetError = projection.side - offsets[index];
      var waveError = projection.forward - wave;
      score += (waveError * waveError * 3) + (offsetError * offsetError);
    });

    return score;
  }

  function candidatePoints(points, firstIndex, firstDirection, secondIndex, secondDirection) {
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

  function chooseNetMove(bui, heading, phase) {
    var points = absoluteExls(bui);
    var currentScore = bodyScore(bui.center, points, heading, phase);
    var best = null;

    if (points.length < 2) {
      return null;
    }

    for (var first = 0; first < points.length - 1; first += 1) {
      for (var second = first + 1; second < points.length; second += 1) {
        DIRECTIONS.forEach(function (firstDirection) {
          DIRECTIONS.forEach(function (secondDirection) {
            var movedPoints = candidatePoints(points, points[first].index, firstDirection, points[second].index, secondDirection);
            var score;

            if (!locallyValid(bui.center, movedPoints)) {
              return;
            }

            score = bodyScore(bui.center, movedPoints, heading, phase);
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
        reason: "no-local-improvement",
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

  function headingFrom(state, momentum) {
    if (magnitude(momentum) > 0.05) {
      state.heading = normalize(momentum);
    }
    if (!state.heading) {
      state.heading = { x: 1, y: 0 };
    }
    return state.heading;
  }

  function survivalPulse(momentum, heading) {
    var speed = magnitude(momentum);
    var direction = speed > 0.01 ? normalize(momentum) : heading;
    var thrust = 0;

    if (speed < 0.18) {
      thrust = 0.04;
    }

    if (thrust <= 0) {
      return null;
    }

    return {
      x: direction.x * thrust,
      y: direction.y * thrust
    };
  }

  window.WebPixelWorld.registerApp({
    id: "eve",
    tick: function (world) {
      var state = world.loadState();
      var bui = world.getBui();
      var heading = headingFrom(state, bui.momentum);
      var phase = Number(state.phase) || 0;
      var thrust = survivalPulse(bui.momentum, heading);
      var powerResult = thrust ? world.applyPower(thrust) : null;
      var plan = chooseNetMove(bui, heading, phase);
      var result = null;

      if (plan && !plan.skipped && typeof world.moveExls === "function") {
        result = world.moveExls(plan.moves);
      }

      state.phase = phase + 1;
      state.lastThrust = thrust;
      state.lastThrustResult = powerResult;
      state.lastBodyMove = {
        plan: plan,
        result: result,
        heading: heading,
        center: bui.center,
        moveCostMoves: bui.moveCostMoves
      };
      world.saveState(state);
    }
  });
})();
