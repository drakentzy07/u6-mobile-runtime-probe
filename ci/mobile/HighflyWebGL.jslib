mergeInto(LibraryManager.library, {
  HF_IsTouchDevice: function () {
    try {
      var ua = navigator.userAgent || '';
      var coarse = window.matchMedia && window.matchMedia('(pointer: coarse)').matches;
      var touchPoints = navigator.maxTouchPoints || 0;
      var mobileUa = /Android|iPhone|iPad|iPod|Mobile/i.test(ua);
      var fine = window.matchMedia && window.matchMedia('(pointer: fine)').matches;
      // A Windows PC can report maxTouchPoints > 0 while still using a real mouse.
      return (mobileUa || (coarse && !fine && touchPoints > 0)) ? 1 : 0;
    } catch (e) {
      return 0;
    }
  },

  HF_ConfigureCanvas: function () {
    try {
      var canvas = Module['canvas'];
      if (!canvas) return;

      var ua = navigator.userAgent || '';
      var coarse = window.matchMedia && window.matchMedia('(pointer: coarse)').matches;
      var touchPoints = navigator.maxTouchPoints || 0;
      var mobileUa = /Android|iPhone|iPad|iPod|Mobile/i.test(ua);
      var fine = window.matchMedia && window.matchMedia('(pointer: fine)').matches;
      var isTouch = mobileUa || (coarse && !fine && touchPoints > 0);

      canvas.style.touchAction = 'none';
      canvas.style.webkitUserSelect = 'none';
      canvas.style.userSelect = 'none';
      canvas.style.cursor = 'auto';
      canvas.style.position = 'fixed';
      canvas.style.margin = '0';
      canvas.style.padding = '0';
      canvas.oncontextmenu = function () { return false; };

      document.documentElement.style.margin = '0';
      document.documentElement.style.padding = '0';
      document.documentElement.style.width = '100%';
      document.documentElement.style.height = '100%';
      document.documentElement.style.overflow = 'hidden';
      document.documentElement.style.background = '#05070b';

      document.body.style.margin = '0';
      document.body.style.padding = '0';
      document.body.style.width = '100%';
      document.body.style.height = '100%';
      document.body.style.overflow = 'hidden';
      document.body.style.overscrollBehavior = 'none';
      document.body.style.background = '#05070b';

      if (isTouch) {
        canvas.style.width = '100vw';
        canvas.style.height = '100vh';
        canvas.style.left = '0';
        canvas.style.top = '0';
        canvas.style.transform = 'none';
        canvas.style.border = '0';
        canvas.style.borderRadius = '0';
        canvas.style.boxShadow = 'none';
      } else {
        var fitDesktopPhone = function () {
          var aspect = 1180 / 560;
          var maxW = Math.min(window.innerWidth * 0.96, 1180);
          var maxH = Math.min(window.innerHeight * 0.92, 560);
          var w = Math.min(maxW, maxH * aspect);
          var h = w / aspect;
          canvas.style.width = Math.round(w) + 'px';
          canvas.style.height = Math.round(h) + 'px';
          canvas.style.left = '50%';
          canvas.style.top = '50%';
          canvas.style.transform = 'translate(-50%, -50%)';
          canvas.style.border = '1px solid rgba(80,190,255,.35)';
          canvas.style.borderRadius = '10px';
          canvas.style.boxShadow = '0 18px 70px rgba(0,0,0,.65)';
        };
        fitDesktopPhone();
        if (!canvas.__highflyDesktopFitBound) {
          canvas.__highflyDesktopFitBound = true;
          window.addEventListener('resize', fitDesktopPhone);
        }
      }

      if (document.pointerLockElement && document.exitPointerLock) {
        document.exitPointerLock();
      }

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
