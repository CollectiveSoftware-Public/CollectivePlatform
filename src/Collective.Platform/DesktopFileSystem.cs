// SPDX-License-Identifier: GPL-3.0-or-later
using Collective.Platform.Abstractions;

namespace Collective.Platform;

/// <summary>
/// Desktop <see cref="IFileSystem"/> rooted at a per-user, per-product app data directory
/// (e.g. <c>%AppData%/CollectiveCode</c>). Pass the product name when registering it.
/// </summary>
public sealed class DesktopFileSystem : IFileSystem
{
    public DesktopFileSystem(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name is required.", nameof(productName));

        string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        AppDataDirectory = Path.Combine(root, productName);
        Directory.CreateDirectory(AppDataDirectory);
    }

    public string AppDataDirectory { get; }

    public bool FileExists(string path) => File.Exists(path);

    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
        => File.ReadAllTextAsync(path, cancellationToken);

    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
        => File.WriteAllTextAsync(path, contents, cancellationToken);
}
