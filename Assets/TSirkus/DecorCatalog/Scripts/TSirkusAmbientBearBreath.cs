using UnityEngine;
namespace TSirkus.Decoration {
 [DisallowMultipleComponent]
 public sealed class TSirkusAmbientBearBreath : MonoBehaviour {
  [SerializeField] private MeshFilter target;
  [SerializeField, Range(0f, .015f)] private float amplitude = .005f;
  [SerializeField, Range(.08f, .5f)] private float breathsPerSecond = .18f;
  private Mesh original, instance;
  private Vector3[] rest, working;
  private float[] weights;
  private void Awake() {
   if (!target || !target.sharedMesh || !target.sharedMesh.isReadable) { enabled = false; return; }
   original = target.sharedMesh; instance = Instantiate(original); instance.MarkDynamic(); target.sharedMesh = instance;
   rest = instance.vertices; working = (Vector3[])rest.Clone(); weights = new float[rest.Length];
   for (int i=0;i<rest.Length;i++) {
    Vector3 p=rest[i]; weights[i]=Mathf.SmoothStep(0f,1f,(p.y-.35f)/.45f)*Mathf.Exp(-Mathf.Pow((p.z-.05f)/.42f,4f));
   }
   Bounds bounds=instance.bounds; bounds.Expand(.035f); instance.bounds=bounds;
  }
  private void LateUpdate() {
   if (!instance) return;
   float phase=Mathf.Sin(Time.time*breathsPerSecond*Mathf.PI*2f);
   for(int i=0;i<rest.Length;i++) {
    Vector3 p=rest[i]; float delta=amplitude*weights[i]*phase; p.x+=Mathf.Sign(p.x)*delta; p.y+=delta*.2f; working[i]=p;
   }
   instance.vertices=working;
  }
  private void OnDisable() { if(instance && rest!=null) instance.vertices=rest; }
  private void OnDestroy() { if(target && target.sharedMesh==instance) target.sharedMesh=original; if(instance) Destroy(instance); }
 }
}
