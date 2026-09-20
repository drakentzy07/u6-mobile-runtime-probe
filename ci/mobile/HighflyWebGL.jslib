mergeInto(LibraryManager.library, {
  HF_IsTouchDevice: function () {
    try {
      var ua = navigator.userAgent || '';
      var coarse = window.matchMedia && window.matchMedia('(pointer: coarse)').matches;
      var touchPoints = navigator.maxTouchPoints || 0;
      var mobileUa = /Android|iPhone|iPad|iPod|Mobile/i.test(ua);
      return (mobileUa || coarse || touchPoints > 0) ? 1 : 0;
    } catch (e) {
      return 0;
    }
  },

  HF_ConfigureCanvas: function () {
    try {
      var canvas = Module['canvas'];
      if (!canvas) return;

      canvas.style.touchAction = 'none';
      canvas.style.webkitUserSelect = 'none';
      canvas.style.userSelect = 'none';
      canvas.style.cursor = 'auto';
      canvas.style.width = '100vw';
      canvas.style.height = '100vh';
      canvas.style.position = 'fixed';
      canvas.style.left = '0';
      canvas.style.top = '0';
      canvas.style.margin = '0';
      canvas.style.padding = '0';
      canvas.oncontextmenu = function () { return false; };

      document.documentElement.style.margin = '0';
      document.documentElement.style.padding = '0';
      document.documentElement.style.width = '100%';
      document.documentElement.style.height = '100%';
      document.documentElement.style.overflow = 'hidden';
      document.documentElement.style.background = '#000';

      document.body.style.margin = '0';
      document.body.style.padding = '0';
      document.body.style.width = '100%';
      document.body.style.height = '100%';
      document.body.style.overflow = 'hidden';
      document.body.style.overscrollBehavior = 'none';
      document.body.style.background = '#000';

      if (document.pointerLockElement && document.exitPointerLock) {
        document.exitPointerLock();
      }

      var ua = navigator.userAgent || '';
      var coarse = window.matchMedia && window.matchMedia('(pointer: coarse)').matches;
      var touchPoints = navigator.maxTouchPoints || 0;
      var mobileUa = /Android|iPhone|iPad|iPod|Mobile/i.test(ua);
      var isTouch = mobileUa || coarse || touchPoints > 0;

      if (isTouch && !canvas.__highflyImmersiveArmed) {
        canvas.__highflyImmersiveArmed = true;

        var requestImmersive = function () {
          try {
            if (document.pointerLockElement && document.exitPointerLock) {
              document.exitPointerLock();
            }

            var request =
              canvas.requestFullscreen ||
              canvas.webkitRequestFullscreen ||
              canvas.msRequestFullscreen;

            var lockLandscape = function () {
              try {
                if (screen.orientation && screen.orientation.lock) {
                  var o = screen.orientation.lock('landscape');
                  if (o && o.catch) o.catch(function () {});
                }
              } catch (_) {}
            };

            if (request && !document.fullscreenElement && !document.webkitFullscreenElement) {
              var result = request.call(canvas);
              if (result && result.then) {
                result.then(lockLandscape).catch(function () {});
              } else {
                lockLandscape();
              }
            } else {
              lockLandscape();
            }

            canvas.focus();
          } catch (_) {}
        };

        canvas.addEventListener('pointerdown', requestImmersive, { capture: true });
        canvas.addEventListener('touchstart', requestImmersive, { capture: true, passive: true });
      }

      var restoreBrowserCursor = function () {
        try {
          canvas.style.cursor = 'auto';
          if (document.pointerLockElement && document.exitPointerLock) {
            document.exitPointerLock();
          }
        } catch (_) {}
      };

      if (!window.__highflyCursorRestoreBound) {
        window.__highflyCursorRestoreBound = true;
        window.addEventListener('pagehide', restoreBrowserCursor);
        window.addEventListener('blur', restoreBrowserCursor);
        document.addEventListener('visibilitychange', function () {
          if (document.hidden) restoreBrowserCursor();
        });
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

      var lockLandscape = function () {
        try {
          if (screen.orientation && screen.orientation.lock) {
            var orientationResult = screen.orientation.lock('landscape');
            if (orientationResult && orientationResult.catch) {
              orientationResult.catch(function () {});
            }
          }
        } catch (_) {}
      };

      if (request && !document.fullscreenElement && !document.webkitFullscreenElement) {
        var result = request.call(canvas);
        if (result && result.then) {
          result.then(lockLandscape).catch(function () {});
        } else {
          lockLandscape();
        }
      } else {
        lockLandscape();
      }

      canvas.focus();
    } catch (e) {
      console.warn('[HIGHFLY] HF_RequestFullscreen failed', e);
    }
  }
});
