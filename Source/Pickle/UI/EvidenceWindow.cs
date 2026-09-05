using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;
using Verse;

namespace RimWorks.Pickle.UI;

public class EvidenceWindow : Window {
  private readonly IReadOnlyList<string> paths;
  private readonly bool film;
  private Texture2D? image;
  private GameObject? videoObject;
  private VideoPlayer? video;
  private int frame;
  private int loadedFrame = -1;
  private string error = string.Empty;

  public EvidenceWindow(string title, IReadOnlyList<string> paths, bool film = false) {
    optionalTitle = title;
    this.paths = paths;
    this.film = film;
    doCloseX = true;
    resizeable = true;
    draggable = true;
  }

  public override Vector2 InitialSize => new Vector2(Verse.UI.screenWidth * 0.9f, Verse.UI.screenHeight * 0.9f);

  public override void DoWindowContents(Rect inRect) {
    if (loadedFrame != frame) {
      Load();
    }

    Rect picture = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - 40f);
    Texture? texture = film ? video?.texture : image;
    if (texture != null && error.Length == 0) {
      GUI.DrawTexture(picture, texture, ScaleMode.ScaleToFit);
    } else {
      Widgets.Label(picture, error.Length > 0 ? error : "Loading evidence...");
    }

    float y = picture.yMax + 6f;
    if (film && video?.isPrepared == true) {
      if (Widgets.ButtonText(new Rect(inRect.x, y, 80f, 28f), video.isPlaying ? "Pause" : "Play")) {
        if (video.isPlaying) {
          video.Pause();
        } else {
          video.Play();
        }
      }

      float position = Widgets.HorizontalSlider(new Rect(inRect.x + 92f, y, inRect.width - 92f, 28f), (float)video.time, 0f, (float)video.length);
      if (Mathf.Abs(position - (float)video.time) > 0.5f) {
        video.time = position;
      }
    } else if (paths.Count > 1) {
      Widgets.Label(new Rect(inRect.x, y, 100f, 28f), $"{frame + 1} / {paths.Count}");
      frame = Mathf.RoundToInt(Widgets.HorizontalSlider(new Rect(inRect.x + 108f, y, inRect.width - 108f, 28f), frame, 0f, paths.Count - 1));
    }
  }

  public override void PostClose() {
    base.PostClose();
    if (image != null) {
      UnityEngine.Object.Destroy(image);
    }

    if (videoObject != null) {
      UnityEngine.Object.Destroy(videoObject);
    }
  }

  private void Load() {
    loadedFrame = frame;
    error = string.Empty;
    try {
      string path = paths[frame];
      if (film) {
        videoObject = new GameObject("Pickle evidence");
        video = videoObject.AddComponent<VideoPlayer>();
        video.playOnAwake = false;
        video.renderMode = VideoRenderMode.APIOnly;
        video.url = new Uri(Path.GetFullPath(path)).AbsoluteUri;
        video.errorReceived += (_, message) => error = message;
        video.Play();
      } else {
        if (image != null) {
          UnityEngine.Object.Destroy(image);
        }

        byte[] bytes = path.StartsWith("data:image/", StringComparison.Ordinal)
            ? Convert.FromBase64String(path.Substring(path.IndexOf(',') + 1)) : File.ReadAllBytes(path);
        image = new Texture2D(2, 2);
        if (!image.LoadImage(bytes)) {
          throw new IOException("Cannot decode this image.");
        }
      }
    } catch (Exception ex) {
      error = ex.Message;
    }
  }
}
