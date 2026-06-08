(function () {
  function loadScript(src) {
    return new Promise(function (resolve, reject) {
      var script = document.createElement("script");
      script.src = src;
      script.onload = resolve;
      script.onerror = function () {
        reject(new Error("Could not load app script: " + src));
      };
      document.body.appendChild(script);
    });
  }

  async function boot() {
    var worldNode = document.getElementById("world");
    var controlsNode = document.getElementById("app-controls");
    var tickReadout = document.getElementById("tick-readout");
    var apps = window.MBOX_APPS || [];

    var world = window.WebPixelWorld.create({
      element: worldNode,
      controlsElement: controlsNode,
      addPillsButton: document.getElementById("add-pills"),
      resetButton: document.getElementById("reset-world"),
      turboButton: document.getElementById("turbo-world"),
      tickReadout: tickReadout,
      apps: apps
    });

    for (var index = 0; index < apps.length; index += 1) {
      await loadScript(apps[index].script);
    }

    world.start();
  }

  boot().catch(function (error) {
    var controlsNode = document.getElementById("app-controls");
    if (controlsNode) {
      controlsNode.textContent = error.message;
    }
  });
})();
