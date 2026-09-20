mergeInto(LibraryManager.library, {
  HF_ConfigureCanvas: function () {
    try {
      var canvas = Module['canvas'];
      if (!canvas) return;

      canvas.style.touchAction = 'none';
      canvas.style.webkitUserSelect = 'none';
      canvas.style.userSelect = 'none';
      canvas.style.cursor = 'none';
      canvas.oncontextmenu = function () { return false; };

      document.documentElement.style.margin = '0';
      document.documentElement.style.padding = '0';
      document.documentElement.style.overflow = 'hidden';
      document.body.style.margin = '0';
      document.body.style.padding = '0';
      document.body.style.overflow = 'hidden';

      if (document.pointerLockElement && document.exitPointerLock) {
        document.exitPointerLock();
      }
    } catch (e) {
      console.warn('[HIGHFLY] HF_ConfigureCanvas failed', e);
    }
  },

  HF_RequestFullscreen: function () {
    try {
      var canvas = Module['canvas'];
      if (!canvas) return;

      if (document.pointerLockElement && document.exitPointerLock) {
        document.exitPointerLock();
      }

      var request =
        canvas.requestFullscreen ||
        canvas.webkitRequestFullscreen ||
        canvas.msRequestFullscreen;

      if (request && !document.fullscreenElement && !document.webkitFullscreenElement) {
        var result = request.call(canvas);
        if (result && result.catch) result.catch(function () {});
      }

      if (screen.orientation && screen.orientation.lock) {
        var orientationResult = screen.orientation.lock('landscape');
        if (orientationResult && orientationResult.catch) {
          orientationResult.catch(function () {});
        }
      }

      canvas.focus();
    } catch (e) {
      console.warn('[HIGHFLY] HF_RequestFullscreen failed', e);
    }
  }
});
