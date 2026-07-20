// SPDX-License-Identifier: GPL-3.0-or-later
namespace Collective.Platform.Abstractions;

/// <summary>Loads/saves a JSON settings object under <see cref="IFileSystem.AppDataDirectory"/>.</summary>
public interface ISettingsStore
{
    Task<T?> LoadAsync<T>(string name, CancellationToken cancellationToken = default) where T : class;
    Task SaveAsync<T>(string name, T value, CancellationToken cancellationToken = default) where T : class;
}
