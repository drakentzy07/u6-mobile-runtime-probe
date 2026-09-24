mergeInto(LibraryManager.library, {
  HF_TouchBridgeInit: function () {
    try {
      var canvas = Module['canvas'];
      if (!canvas) return;

      canvas.style.touchAction = 'none';
      canvas.style.webkitUserSelect = 'none';
      canvas.style.userSelect = 'none';
      canvas.style.overscrollBehavior = 'none';

      document.documentElement.style.overscrollBehavior = 'none';
      document.body.style.overscrollBehavior = 'none';

      if (!Module.__hfTouchState) {
        Module.__hfTouchState = { list: [] };
      }

      if (canvas.__hfTouchBridgeBound) return;
      canvas.__hfTouchBridgeBound = true;

      var refresh = function (event) {
        try {
          if (event.cancelable) event.preventDefault();

          var rect = canvas.getBoundingClientRect();
          var width = Math.max(1, rect.width);
          var height = Math.max(1, rect.height);
          var list = [];

          for (var i = 0; i < event.touches.length; i++) {
            var t = event.touches[i];
            var nx = (t.clientX - rect.left) / width;
            var ny = 1.0 - ((t.clientY - rect.top) / height);

            nx = Math.max(0, Math.min(1, nx));
            ny = Math.max(0, Math.min(1, ny));

            list.push({
              id: t.identifier | 0,
              x: (nx * 100000) | 0,
              y: (ny * 100000) | 0
            });
          }

          Module.__hfTouchState.list = list;
        } catch (e) {
          console.warn('[HIGHFLY] touch refresh failed', e);
        }
      };

      canvas.addEventListener('touchstart', refresh, { capture: true, passive: false });
      canvas.addEventListener('touchmove', refresh, { capture: true, passive: false });
      canvas.addEventListener('touchend', refresh, { capture: true, passive: false });
      canvas.addEventListener('touchcancel', refresh, { capture: true, passive: false });

      console.log('[HIGHFLY] direct WebGL touch bridge ready');
    } catch (e) {
      console.warn('[HIGHFLY] HF_TouchBridgeInit failed', e);
    }
  },

  HF_TouchCount: function () {
    try {
      var state = Module.__hfTouchState;
      return state && state.list ? state.list.length : 0;
    } catch (e) {
      return 0;
    }
  },

  HF_TouchIdAt: function (index) {
    try {
      var state = Module.__hfTouchState;
      if (!state || !state.list || index < 0 || index >= state.list.length) return -1;
      return state.list[index].id | 0;
    } catch (e) {
      return -1;
    }
  },

  HF_TouchX100kAt: function (index) {
    try {
      var state = Module.__hfTouchState;
      if (!state || !state.list || index < 0 || index >= state.list.length) return 0;
      return state.list[index].x | 0;
    } catch (e) {
      return 0;
    }
  },

  HF_TouchY100kAt: function (index) {
    try {
      var state = Module.__hfTouchState;
      if (!state || !state.list || index < 0 || index >= state.list.length) return 0;
      return state.list[index].y | 0;
    } catch (e) {
      return 0;
    }
  }
});
