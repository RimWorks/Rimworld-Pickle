using System;
using Xunit;

namespace RimWorks.Pickle.Game.Tests;

public sealed class GameFixture : IDisposable {
  public GameFixture() {
    Session = GameSession.Start();
  }

  public GameSession Session { get; }

  public void Dispose() {
    Session.Dispose();
  }
}

[CollectionDefinition(Name)]
public class GameCollection : ICollectionFixture<GameFixture> {
  public const string Name = "game";
}
