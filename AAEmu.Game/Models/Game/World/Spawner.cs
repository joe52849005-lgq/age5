using System.Threading.Tasks;

using AAEmu.Game.Models.Game.World.Transform;

namespace AAEmu.Game.Models.Game.World;

public class Spawner<T> where T : GameObject
{
    public uint Id { get; set; }        // index
    public uint SpawnerId { get; set; } // spawner template id
    public uint UnitId { get; set; }    // npc template id
    public string FollowPath { get; set; } = string.Empty;
    public uint FollowNpc { get; set; } = 0; // nearest Npc TemplateId to follow
    public WorldSpawnPosition Position { get; set; }
    public int RespawnTime { get; set; } = 15;
    public int DespawnTime { get; set; } = 20;

    public virtual T Spawn(uint objId)
    {
        return null;
    }

    public virtual T Spawn(uint objId, ulong itemId, uint charId)
    {
        return null;
    }

    public virtual void Respawn(T obj)
    {
        Spawn(0);
    }

    public virtual void Despawn(T obj)
    {
    }

    /// <summary>
    /// Spawns an object asynchronously.
    /// </summary>
    /// <param name="objId">The object ID to spawn.</param>
    /// <returns>The spawned object, or null if spawning failed.</returns>
    public virtual Task<T> SpawnAsync(uint objId)
    {
        return Task.FromResult<T>(null);
    }

    /// <summary>
    /// Spawns an object asynchronously with additional parameters.
    /// </summary>
    /// <param name="objId">The object ID to spawn.</param>
    /// <param name="itemId">The item ID associated with the spawn.</param>
    /// <param name="charId">The character ID associated with the spawn.</param>
    /// <returns>The spawned object, or null if spawning failed.</returns>
    public virtual Task<T> SpawnAsync(uint objId, ulong itemId, uint charId)
    {
        return Task.FromResult<T>(null);
    }

    /// <summary>
    /// Respawns an object asynchronously.
    /// </summary>
    /// <param name="obj">The object to respawn.</param>
    public virtual async Task RespawnAsync(T obj)
    {
        await SpawnAsync(0);
    }

    /// <summary>
    /// Despawns an object asynchronously.
    /// </summary>
    /// <param name="obj">The object to despawn.</param>
    public virtual Task DespawnAsync(T obj)
    {
        return Task.CompletedTask;
    }
}
