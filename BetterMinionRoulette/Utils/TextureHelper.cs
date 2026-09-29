namespace NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette.Util;

using System;
using System.Collections.Generic;
using Dalamud.Game;
using Dalamud.Interface.Textures;
using Dalamud.Bindings.ImGui;
using NekoBoiNick.FFXIV.DalamudPlugin.BetterMinionRoulette;

internal sealed class TextureHelper {
  private readonly Dictionary<string, ISharedImmediateTexture> _loadedTextures = new();
  private readonly Dictionary<uint, ISharedImmediateTexture> _loadedIconTextures = new();

  public ImTextureID LoadUldTexture(string name) {
    string path = $"ui/uld/{name}_hr1.tex";
    return LoadTexture(_loadedTextures, path, Services.TextureProvider.GetFromGame);
  }

  public ImTextureID LoadIconTexture(uint id) {
    return LoadTexture(_loadedIconTextures, id, x => Services.TextureProvider.GetFromGameIcon(new GameIconLookup {
      IconId = id,
      HiRes = true,
      Language = ClientLanguage.English
    }));
  }

  public void Dispose() {
    _loadedTextures.Clear();
    _loadedIconTextures.Clear();
  }

  private static ImTextureID LoadTexture<TKey>(Dictionary<TKey, ISharedImmediateTexture> cache, TKey key, Func<TKey, ISharedImmediateTexture> loadFunc) where TKey : notnull {
    if (!cache.TryGetValue(key, out ISharedImmediateTexture? texture)) {
      texture = loadFunc(key);
      cache[key] = texture;
    }

    // The empty handle is temporary while Dalamud loads the texture. Resolve the
    // wrap on every draw instead of permanently caching that initial empty handle.
    return texture.GetWrapOrEmpty().Handle;
  }
}
