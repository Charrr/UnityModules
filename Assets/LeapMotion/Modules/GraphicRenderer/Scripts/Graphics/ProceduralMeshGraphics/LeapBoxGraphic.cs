/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

namespace Leap.Unity.GraphicalRenderer {

  /// <summary>
  /// TODO: Document me!
  /// </summary>
  [DisallowMultipleComponent]
  public class LeapBoxGraphic : LeapMeshGraphicBase {

    public const int MAX_VERTS_PER_AXIS = 128;

    [EditTimeOnly]
    [SerializeField]
    private int _sourceDataIndex = -1;

    [Tooltip("Specifies whether or not this panel has a specific resolution, or whether this " +
             "panel automatically changes its resolution based on its size")]
    [EditTimeOnly]
    [SerializeField]
    private ResolutionType _resolutionType = ResolutionType.VerticesPerRectilinearMeter;

    [HideInInspector]
    [SerializeField]
    private int _resolution_vert_x, _resolution_vert_y;

    [EditTimeOnly]
    [SerializeField]
    private Vector2 _resolution_verts_per_meter = new Vector2(20, 20);

    [MinValue(0)]
    [EditTimeOnly]
    [SerializeField]
    private Vector3 _size = new Vector3(0.1f, 0.1f, 0.01f);
    /// <summary>
    /// Gets the dimensions of the box graphic in local space.
    /// </summary>
    public Vector3 size {
      get { return _size; }
    }

    [Tooltip("Uses sprite data to generate a nine sliced panel.")]
    [EditTimeOnly]
    [SerializeField]
    private bool _nineSliced = false;

    /// <summary>
    /// Returns whether or not a feature data object is a valid object
    /// that can be used to drive texture data for this panel.  Only
    /// a TextureData object or a SpriteData object are currently valid.
    /// </summary>
    public static bool IsValidDataSource(LeapFeatureData dataSource) {
      return dataSource is LeapTextureData ||
             dataSource is LeapSpriteData;
    }

    /// <summary>
    /// Returns the current feature data object being used as source.
    /// </summary>
    public LeapFeatureData sourceData {
      get {
        if (_sourceDataIndex == -1) {
          assignDefaultSourceValue();
        }
        if (_sourceDataIndex < 0 || _sourceDataIndex >= featureData.Count) {
          return null;
        }
        return featureData[_sourceDataIndex];
      }
#if UNITY_EDITOR
      set {
        _sourceDataIndex = _featureData.IndexOf(value);
        setSourceFeatureDirty();
      }
#endif
    }

    /// <summary>
    /// Returns the current resolution type being used for this panel.
    /// </summary>
    public ResolutionType resolutionType {
      get {
        return _resolutionType;
      }
    }

    /// <summary>
    /// Returns the current local-space rect of this panel.  If there is a
    /// RectTransform attached to this panel, this value is the same as calling
    /// rectTransform.rect.
    /// </summary>
    public Rect rect {
      get {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null) {
          _size = new Vector3(rectTransform.rect.size.x,
                              rectTransform.rect.size.y,
                              _size.z);
          return rectTransform.rect;
        }
        else {
          return new Rect(-_size / 2, _size);
        }
      }
    }

    /// <summary>
    /// Gets or sets whether or not this panel is currently using nine slicing.
    /// </summary>
    public bool nineSliced {
      get {
        return _nineSliced && canNineSlice;
      }
      set {
        _nineSliced = value;
        setSourceFeatureDirty();
      }
    }

    /// <summary>
    /// Returns whether or not the current source supports nine slicing.
    /// </summary>
    public bool canNineSlice {
      get {
        var spriteData = sourceData as LeapSpriteData;
        return spriteData != null && spriteData.sprite != null;
      }
    }

    /// <summary>
    /// Returns which uv channel is being used for this panel.  It will
    /// always match the uv channel being used by the source.
    /// </summary>
    public UVChannelFlags uvChannel {
      get {
        if (sourceData == null) {
          return UVChannelFlags.UV0;
        }

        var feature = sourceData.feature;
        if (feature is LeapTextureFeature) {
          return (feature as LeapTextureFeature).channel;
        }
        else if (feature is LeapSpriteFeature) {
          return (feature as LeapSpriteFeature).channel;
        }
        else {
          return UVChannelFlags.UV0;
        }
      }
    }

    protected override void Reset() {
      base.Reset();

      assignDefaultSourceValue();
      setSourceFeatureDirty();
    }

    protected override void OnValidate() {
      base.OnValidate();

      if (sourceData == null) {
        assignDefaultSourceValue();
      }

      _resolution_vert_x = Mathf.Max(0, _resolution_vert_x);
      _resolution_vert_y = Mathf.Max(0, _resolution_vert_y);
      _resolution_verts_per_meter = Vector2.Max(_resolution_verts_per_meter, Vector2.zero);

      if (_resolutionType == ResolutionType.Vertices) {
        _resolution_verts_per_meter.x = _resolution_vert_x / rect.width;
        _resolution_verts_per_meter.y = _resolution_vert_y / rect.height;
      }
      else {
        _resolution_vert_x = Mathf.RoundToInt(_resolution_verts_per_meter.x * rect.width);
        _resolution_vert_y = Mathf.RoundToInt(_resolution_verts_per_meter.y * rect.height);
      }

      setSourceFeatureDirty();
    }

    public override void RefreshMeshData() {
      if (sourceData == null) {
        assignDefaultSourceValue();
      }

      Vector4 borderSize = Vector4.zero;
      Vector4 borderUvs = Vector4.zero;

      Rect rect;
      RectTransform rectTransform = GetComponent<RectTransform>();
      if (rectTransform != null) {
        rect = rectTransform.rect;
        _size = new Vector3(rect.size.x,
                            rect.size.y,
                            _size.z);
      }
      else {
        rect = new Rect(-_size / 2, _size);
      }

      if (_nineSliced && sourceData is LeapSpriteData) {
        var spriteData = sourceData as LeapSpriteData;
        if (spriteData.sprite == null) {
          mesh = null;
          remappableChannels = 0;
          return;
        }

        var sprite = spriteData.sprite;

        Vector4 border = sprite.border;
        borderSize = border / sprite.pixelsPerUnit;

        borderUvs = border;
        borderUvs.x /= sprite.textureRect.width;
        borderUvs.z /= sprite.textureRect.width;
        borderUvs.y /= sprite.textureRect.height;
        borderUvs.w /= sprite.textureRect.height;
      }

      List<Vector3> verts = new List<Vector3>();
      List<Vector2> uvs = new List<Vector2>();
      List<int> tris = new List<int>();

      List<Vector3> normals = new List<Vector3>();

      int vertsX, vertsY;
      if (_resolutionType == ResolutionType.Vertices) {
        vertsX = Mathf.RoundToInt(_resolution_vert_x);
        vertsY = Mathf.RoundToInt(_resolution_vert_y);
      }
      else {
        vertsX = Mathf.RoundToInt(rect.width * _resolution_verts_per_meter.x);
        vertsY = Mathf.RoundToInt(rect.height * _resolution_verts_per_meter.y);
      }

      vertsX += _nineSliced ? 4 : 2;
      vertsY += _nineSliced ? 4 : 2;

      vertsX = Mathf.Min(vertsX, MAX_VERTS_PER_AXIS);
      vertsY = Mathf.Min(vertsY, MAX_VERTS_PER_AXIS);

      // Back
      for (int vy = 0; vy < vertsY; vy++) {
        for (int vx = 0; vx < vertsX; vx++) {
          Vector2 vert;
          vert.x = calculateVertAxis(vx, vertsX, rect.width, borderSize.x, borderSize.z);
          vert.y = calculateVertAxis(vy, vertsY, rect.height, borderSize.y, borderSize.w);
          verts.Add(vert + new Vector2(rect.x, rect.y));
          normals.Add(Vector3.forward);

          Vector2 uv;
          uv.x = calculateVertAxis(vx, vertsX, 1, borderUvs.x, borderUvs.z);
          uv.y = calculateVertAxis(vy, vertsY, 1, borderUvs.y, borderUvs.w);
          uvs.Add(uv);
        }
      }

      int backVertsCount = verts.Count;

      // Front
      float depth = -_size.z;
      for (int vy = 0; vy < vertsY; vy++) {
        for (int vx = 0; vx < vertsX; vx++) {
          Vector3 vert = Vector3.zero;
          vert.x = calculateVertAxis(vx, vertsX, rect.width, borderSize.x, borderSize.z);
          vert.y = calculateVertAxis(vy, vertsY, rect.height, borderSize.y, borderSize.w);
          verts.Add(vert + new Vector3(rect.x, rect.y, depth));
          normals.Add(Vector3.back);

          Vector2 uv;
          uv.x = calculateVertAxis(vx, vertsX, 1, borderUvs.x, borderUvs.z);
          uv.y = calculateVertAxis(vy, vertsY, 1, borderUvs.y, borderUvs.w);
          uvs.Add(uv);
        }
      }

      // Back
      for (int vy = 0; vy < vertsY - 1; vy++) {
        for (int vx = 0; vx < vertsX - 1; vx++) {
          int vertIndex = vy * vertsX + vx;

          tris.Add(vertIndex);
          tris.Add(vertIndex + 1);
          tris.Add(vertIndex + 1 + vertsX);

          tris.Add(vertIndex);
          tris.Add(vertIndex + 1 + vertsX);
          tris.Add(vertIndex + vertsX);
        }
      }

      // Front
      for (int vy = 0; vy < vertsY - 1; vy++) {
        for (int vx = 0; vx < vertsX - 1; vx++) {
          int vertIndex = backVertsCount + (vy * vertsX + vx);

          tris.Add(vertIndex);
          tris.Add(vertIndex + 1 + vertsX);
          tris.Add(vertIndex + 1);

          tris.Add(vertIndex);
          tris.Add(vertIndex + vertsX);
          tris.Add(vertIndex + 1 + vertsX);
        }
      }

      // Edges
      int ex = 0, ey = 0;
      int backVertIdx = verts.Count, frontVertIdx = verts.Count;

      // Left
      for (int vy = 0; vy < vertsY; vy++) { // Repeat back edge, left side
        Vector2 vert;
        vert.x = calculateVertAxis(ex, vertsX, rect.width, borderSize.x, borderSize.z);
        vert.y = calculateVertAxis(vy, vertsY, rect.height, borderSize.y, borderSize.w);
        verts.Add(vert + new Vector2(rect.x, rect.y));
        normals.Add(Vector3.left);

        frontVertIdx += 1;

        Vector2 uv;
        uv.x = calculateVertAxis(ex, vertsX, 1, borderUvs.x, borderUvs.z);
        uv.y = calculateVertAxis(vy, vertsY, 1, borderUvs.y, borderUvs.w);
        uvs.Add(uv);
      }
      for (int vy = 0; vy < vertsY; vy++) { // Repeat front edge, left side
        Vector3 vert = Vector3.zero;
        vert.x = calculateVertAxis(ex, vertsX, rect.width, borderSize.x, borderSize.z);
        vert.y = calculateVertAxis(vy, vertsY, rect.height, borderSize.y, borderSize.w);
        verts.Add(vert + new Vector3(rect.x, rect.y, depth));
        normals.Add(Vector3.left);

        Vector2 uv;
        uv.x = calculateVertAxis(ex, vertsX, 1, borderUvs.x, borderUvs.z);
        uv.y = calculateVertAxis(vy, vertsY, 1, borderUvs.y, borderUvs.w);
        uvs.Add(uv);
      }
      for (int vy = 0; vy < vertsY - 1; vy++) { // Add quads
        addQuad(tris, frontVertIdx + vy, backVertIdx + vy, backVertIdx + vy + 1, frontVertIdx + vy + 1);
      }

      // Right
      ex = vertsX - 1;
      backVertIdx = verts.Count;
      frontVertIdx = verts.Count;
      for (int vy = 0; vy < vertsY; vy++) { // Repeat back edge, right side
        Vector2 vert;
        vert.x = calculateVertAxis(ex, vertsX, rect.width, borderSize.x, borderSize.z);
        vert.y = calculateVertAxis(vy, vertsY, rect.height, borderSize.y, borderSize.w);
        verts.Add(vert + new Vector2(rect.x, rect.y));
        normals.Add(Vector3.right);

        frontVertIdx += 1;

        Vector2 uv;
        uv.x = calculateVertAxis(ex, vertsX, 1, borderUvs.x, borderUvs.z);
        uv.y = calculateVertAxis(vy, vertsY, 1, borderUvs.y, borderUvs.w);
        uvs.Add(uv);
      }
      for (int vy = 0; vy < vertsY; vy++) { // Repeat front edge, right side
        Vector3 vert = Vector3.zero;
        vert.x = calculateVertAxis(ex, vertsX, rect.width, borderSize.x, borderSize.z);
        vert.y = calculateVertAxis(vy, vertsY, rect.height, borderSize.y, borderSize.w);
        verts.Add(vert + new Vector3(rect.x, rect.y, depth));
        normals.Add(Vector3.right);

        Vector2 uv;
        uv.x = calculateVertAxis(ex, vertsX, 1, borderUvs.x, borderUvs.z);
        uv.y = calculateVertAxis(vy, vertsY, 1, borderUvs.y, borderUvs.w);
        uvs.Add(uv);
      }
      for (int vy = 0; vy < vertsY - 1; vy++) { // Add quads
        addQuad(tris, frontVertIdx + vy + 1, backVertIdx + vy + 1, backVertIdx + vy, frontVertIdx + vy);
      }

      // Top
      ey = vertsY - 1;
      backVertIdx = verts.Count;
      frontVertIdx = verts.Count;
      for (int vx = 0; vx < vertsX; vx++) { // Repeat back edge, upper side
        Vector2 vert;
        vert.x = calculateVertAxis(vx, vertsX, rect.width, borderSize.x, borderSize.z);
        vert.y = calculateVertAxis(ey, vertsY, rect.height, borderSize.y, borderSize.w);
        verts.Add(vert + new Vector2(rect.x, rect.y));
        normals.Add(Vector3.up);

        frontVertIdx += 1;

        Vector2 uv;
        uv.x = calculateVertAxis(vx, vertsX, 1, borderUvs.x, borderUvs.z);
        uv.y = calculateVertAxis(ey, vertsY, 1, borderUvs.y, borderUvs.w);
        uvs.Add(uv);
      }
      for (int vx = 0; vx < vertsX; vx++) { // Repeat front edge, upper side
        Vector3 vert = Vector3.zero;
        vert.x = calculateVertAxis(vx, vertsX, rect.width, borderSize.x, borderSize.z);
        vert.y = calculateVertAxis(ey, vertsY, rect.height, borderSize.y, borderSize.w);
        verts.Add(vert + new Vector3(rect.x, rect.y, depth));
        normals.Add(Vector3.up);

        Vector2 uv;
        uv.x = calculateVertAxis(vx, vertsX, 1, borderUvs.x, borderUvs.z);
        uv.y = calculateVertAxis(ey, vertsY, 1, borderUvs.y, borderUvs.w);
        uvs.Add(uv);
      }
      for (int vx = 0; vx < vertsX - 1; vx++) { // Add quads
        addQuad(tris, frontVertIdx + vx, backVertIdx + vx, backVertIdx + vx + 1, frontVertIdx + vx + 1);
      }

      // Bottom
      ey = 0;
      backVertIdx = verts.Count;
      frontVertIdx = verts.Count;
      for (int vx = 0; vx < vertsX; vx++) { // Repeat back edge, upper side
        Vector2 vert;
        vert.x = calculateVertAxis(vx, vertsX, rect.width, borderSize.x, borderSize.z);
        vert.y = calculateVertAxis(ey, vertsY, rect.height, borderSize.y, borderSize.w);
        verts.Add(vert + new Vector2(rect.x, rect.y));
        normals.Add(Vector3.down);

        frontVertIdx += 1;

        Vector2 uv;
        uv.x = calculateVertAxis(vx, vertsX, 1, borderUvs.x, borderUvs.z);
        uv.y = calculateVertAxis(ey, vertsY, 1, borderUvs.y, borderUvs.w);
        uvs.Add(uv);
      }
      for (int vx = 0; vx < vertsX; vx++) { // Repeat front edge, upper side
        Vector3 vert = Vector3.zero;
        vert.x = calculateVertAxis(vx, vertsX, rect.width, borderSize.x, borderSize.z);
        vert.y = calculateVertAxis(ey, vertsY, rect.height, borderSize.y, borderSize.w);
        verts.Add(vert + new Vector3(rect.x, rect.y, depth));
        normals.Add(Vector3.down);

        Vector2 uv;
        uv.x = calculateVertAxis(vx, vertsX, 1, borderUvs.x, borderUvs.z);
        uv.y = calculateVertAxis(ey, vertsY, 1, borderUvs.y, borderUvs.w);
        uvs.Add(uv);
      }
      for (int vx = 0; vx < vertsX - 1; vx++) { // Add quads
        addQuad(tris, frontVertIdx + vx + 1, backVertIdx + vx + 1, backVertIdx + vx, frontVertIdx + vx);
      }

      mesh = new Mesh();
      mesh.name = "Box Mesh";
      mesh.hideFlags = HideFlags.HideAndDontSave;
      mesh.SetVertices(verts);
      mesh.SetNormals(normals);
      mesh.SetTriangles(tris, 0);
      mesh.SetUVs(uvChannel.Index(), uvs);
      mesh.RecalculateBounds();

      remappableChannels = UVChannelFlags.UV0;
    }

    private void addQuad(List<int> tris, int idx0, int idx1, int idx2, int idx3) {
      tris.Add(idx0);
      tris.Add(idx1);
      tris.Add(idx2);

      tris.Add(idx0);
      tris.Add(idx2);
      tris.Add(idx3);
    }

    private float calculateVertAxis(int dv, int vertCount, float size, float border0, float border1) {
      if (_nineSliced) {
        if (dv == 0) {
          return 0;
        }
        else if (dv == (vertCount - 1)) {
          return size;
        }
        else if (dv == 1) {
          return border0;
        }
        else if (dv == (vertCount - 2)) {
          return size - border1;
        }
        else {
          return ((dv - 1.0f) / (vertCount - 3.0f)) * (size - border0 - border1) + border0;
        }
      }
      else {
        return (dv / (vertCount - 1.0f)) * size;
      }
    }

    private void assignDefaultSourceValue() {
      _sourceDataIndex = featureData.Query().IndexOf(IsValidDataSource);
    }

    private void setSourceFeatureDirty() {
      if (sourceData != null) {
        sourceData.MarkFeatureDirty();
      }
    }

    public enum ResolutionType {
      Vertices,
      VerticesPerRectilinearMeter
    }

    void OnDrawGizmosSelected() {
      Gizmos.matrix = transform.localToWorldMatrix;
      Gizmos.color = new Color(1F, 1F, 1F, 0.4F);
      Gizmos.DrawWireCube(-Vector3.forward * this.size.z / 2F, this.size);
    }
  }
}
