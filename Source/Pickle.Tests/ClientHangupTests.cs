using System;
using System.IO;
using System.Net;
using RimWorks.Pickle.Core.Web;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class ClientHangupTests {
  [Fact]
  public void AWriteToADisposedSocketIsAHangup() {
    HttpListenerException dropped = new HttpListenerException(
      1229, "Unable to write data to the transport connection: Cannot access a disposed object.");

    Assert.True(ClientHangup.Caused(dropped));
  }

  [Fact]
  public void AWrappedHangupStillCounts() {
    InvalidOperationException wrapped = new InvalidOperationException("writing the dashboard", new IOException("broken pipe"));

    Assert.True(ClientHangup.Caused(wrapped));
  }

  [Fact]
  public void ARealFailureIsNotAHangup() {
    Assert.False(ClientHangup.Caused(new InvalidOperationException("route threw")));
  }

  [Fact]
  public void NothingIsNotAHangup() {
    Assert.False(ClientHangup.Caused(null));
  }
}
