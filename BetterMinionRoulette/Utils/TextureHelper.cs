namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Util;

using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette;

internal sealed class TextureHelper {
  private readonly Dictionary<string, IDalamudTextureWrap> _loadedTextures = new();
  private readonly Dictionary<uint, IDalamudTextureWrap> _loadedIconTextures = new();

  public nint LoadUldTexture(string name) {
    string path = $"ui/uld/{name}_hr1.tex";
    return LoadTexture(_loadedTextures, path, Services.TextureProvider.GetFromGame);
  }

  public nint LoadIconTexture(uint id) {
    return LoadTexture(_loadedIconTextures, id, x => Services.TextureProvider.GetFromGameIcon(new GameIconLookup {
      IconId = id,
      HiRes = true,
      Language = ClientLanguage.English
    }));
  }

  public void Dispose() {
    var values = _loadedTextures.Values.Concat(_loadedIconTextures.Values).ToList();
    _loadedTextures.Clear();
    _loadedIconTextures.Clear();
    values.ForEach(x => x.Dispose());
  }

  private static nint LoadTexture<TKey>(Dictionary<TKey, IDalamudTextureWrap> cache, TKey key, Func<TKey, ISharedImmediateTexture> loadFunc) where TKey : notnull {
    if (cache.TryGetValue(key, out IDalamudTextureWrap? texture)) {
      try {
        return texture.ImGuiHandle;
      } catch (ObjectDisposedException disposedException) {
        if (Services.Log.MinimumLogLevel == Serilog.Events.LogEventLevel.Verbose) {
          Services.Log.Error(disposedException, disposedException.Message);
        }
      } catch (Exception exception1) {
        if (Services.Log.MinimumLogLevel == Serilog.Events.LogEventLevel.Verbose) {
          Services.Log.Error(exception1, exception1.Message);
        }
      }
    }

    ISharedImmediateTexture sharedTexture = loadFunc(key);
    if (!sharedTexture.TryGetWrap(out IDalamudTextureWrap? wrap, out Exception? exception2)) {
      if (exception2 is not null && Services.Log.MinimumLogLevel == Serilog.Events.LogEventLevel.Verbose) {
        Services.Log.Error(exception2, exception2.Message);
      }
      return sharedTexture.GetWrapOrEmpty().ImGuiHandle;
    }
    if (exception2 is not null && Services.Log.MinimumLogLevel == Serilog.Events.LogEventLevel.Verbose) {
      Services.Log.Error(exception2, exception2.Message);
    }
    if (wrap is null) {
      return sharedTexture.GetWrapOrEmpty().ImGuiHandle;
    }
    cache[key] = wrap;
    return wrap.ImGuiHandle;
  }
}
