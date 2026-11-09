using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.Rendering;

namespace AllSorted
{
    public sealed partial class SortGameViewController
    {
        private const string CardResource = "Prefabs/Merchandise/CardTransformation/";
        private const float CardEffectDuration = 1.18f;
        private const float FirstBeam = .29f;
        private const float Stagger = .064f;
        private const float BeamDuration = .16f;
        private const float Impact = .10f;
        private static readonly int LightColor = Shader.PropertyToID("_Color");
        private static readonly Color GoldLight = new Color(1f,.84f,.46f);
        private readonly List<CardTargetView> cardTargets = new List<CardTargetView>(9);
        private readonly List<CardSpark> cardSparks = new List<CardSpark>(72);
        private MaterialPropertyBlock cardLightBlock;
        private GameObject cardPrefab, cardFxRoot;
        private Transform cardCaster;
        private readonly List<Transform> pooledCards = new List<Transform>(10);
        private readonly List<Renderer> pooledLights = new List<Renderer>(100);
        private int usedCards, usedLights;
        private Renderer casterGlow;
        private Material beamArt, flashArt, glowArt, sparkleArt;
        private SortBoardState cardBoard;
        private float cardElapsed;
        private Vector3 casterPosition;
        private bool cardAssetsLoaded;

        public bool IsCardTransformationPlaying => cardBoard != null;
        private GameObject CardPrefab
        {
            get
            {
                if (!cardAssetsLoaded)
                {
                    cardAssetsLoaded = true;
                    cardPrefab = Resources.Load<GameObject>(CardResource + "CardItem");
                }
                return cardPrefab;
            }
        }

        private sealed class CardTargetView
        {
            public Vector3 Position;
            public PieceVisual Old;
            public Transform Card;
            public Renderer Glow, Beam, Flash;
            public bool Replaced;
        }

        private sealed class CardSpark
        {
            public Renderer Light;
            public Vector3 Origin, Velocity;
            public float Starts, Duration, Size, Angle;
        }

        private void BeginCardEffect()
        {
            cardBoard = model.Board;
            cardElapsed = 0;
            if (cardLightBlock==null) cardLightBlock=new MaterialPropertyBlock();
            beamArt = Resources.Load<Material>(CardResource + "Beam");
            flashArt = Resources.Load<Material>(CardResource + "Flash");
            glowArt = Resources.Load<Material>(CardResource + "Glow");
            sparkleArt = Resources.Load<Material>(CardResource + "Sparkle");
            if (!beamArt || !flashArt || !glowArt || !sparkleArt)
            {
                EndCardEffect(true);
                return;
            }

            Layers.SetModal(true);
            RefreshBoosterUi(true);
            if (!cardFxRoot)
            {
                cardFxRoot = new GameObject("Card Transformation VFX");
                cardFxRoot.transform.SetParent(transform,false);
            }
            usedCards=0;
            usedLights=0;
            cardFxRoot.SetActive(true);
            var camera = BoardCamera;
            float depth = Mathf.Max(camera.nearClipPlane+.8f,camera.WorldToViewportPoint(PieceRoot.position).z-3f);
            casterPosition = camera.ViewportToWorldPoint(new Vector3(.50f,.65f,depth));
            cardCaster = CreateFxCard("Casting Card",casterPosition);
            casterGlow = CreateLight("Casting Glow",glowArt);
            AddCardSparks(casterPosition,.13f,18,new System.Random(272));
            var random = new System.Random(17);

            foreach (var target in cardBoard.CardTargets)
            {
                pieces.TryGetValue(target.Id,out var old);
                // Buried and locked picks are signalled at their owning slot;
                // revealing the original merchandise would expose hidden stock.
                Vector3 anchor = old != null ? old.Transform.position : StageSlotWorld(target.Shelf,target.Slot);
                Vector3 vp = camera.WorldToViewportPoint(anchor);
                if (old == null && target.Depth > 0) vp.y += Mathf.Min(target.Depth,3)*.012f;
                vp.x = Mathf.Clamp(vp.x,.08f,.92f);
                vp.y = Mathf.Clamp(vp.y,.12f,.88f);
                vp.z = depth;
                Vector3 position = camera.ViewportToWorldPoint(vp);
                int index = cardTargets.Count;
                var view = new CardTargetView
                {
                    Position=position, Old=old, Card=CreateFxCard("Revealed Card "+target.Id,position),
                    Glow=CreateLight("Target Glow",glowArt),
                    Beam=CreateLight("Card Beam",beamArt),
                    Flash=CreateLight("Target Flash",flashArt)
                };
                view.Card.gameObject.SetActive(false);
                cardTargets.Add(view);
                AddCardSparks(position,FirstBeam+index*Stagger+Impact,6,random);
            }
            RenderCardEffect();
        }

        private Transform CreateFxCard(string label,Vector3 position)
        {
            if (usedCards==pooledCards.Count)
            {
                var instance=Instantiate(CardPrefab,cardFxRoot.transform);
                instance.GetComponent<BoxCollider>().enabled=false;
                pooledCards.Add(instance.transform);
            }
            var card=pooledCards[usedCards++];
            card.name=label;
            card.SetPositionAndRotation(position,BoardCamera.transform.rotation);
            card.gameObject.SetActive(true);
            return card;
        }

        private Renderer CreateLight(string label,Material material)
        {
            if (usedLights==pooledLights.Count)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.transform.SetParent(cardFxRoot.transform,false);
                var collider = quad.GetComponent<Collider>();
                collider.enabled = false;
                Destroy(collider);
                var created=quad.GetComponent<MeshRenderer>();
                created.shadowCastingMode=ShadowCastingMode.Off;
                created.receiveShadows=false;
                pooledLights.Add(created);
            }
            var renderer=pooledLights[usedLights++];
            renderer.name=label;
            renderer.sharedMaterial=material;
            renderer.enabled = false;
            return renderer;
        }

        private void AddCardSparks(Vector3 position,float start,int count,System.Random random)
        {
            var basis = BoardCamera.transform;
            for (int i=0;i<count;i++)
            {
                float angle=(float)random.NextDouble()*Mathf.PI*2;
                float speed=.5f+(float)random.NextDouble()*1.5f;
                Vector3 direction=basis.right*Mathf.Cos(angle)+basis.up*Mathf.Sin(angle);
                cardSparks.Add(new CardSpark
                {
                    Light=CreateLight("Card Sparkle",sparkleArt), Origin=position,
                    Starts=start, Duration=.4f+(float)random.NextDouble()*.34f,
                    Size=.05f+(float)random.NextDouble()*.10f, Angle=angle*Mathf.Rad2Deg,
                    Velocity=direction*speed
                });
            }
        }

        private void AdvanceCardEffect()
        {
            if (!ReferenceEquals(cardBoard,model.Board))
            {
                EndCardEffect(true);
                return;
            }
            cardElapsed += Time.unscaledDeltaTime;
            if (cardElapsed >= CardEffectDuration)
            {
                EndCardEffect(true);
                return;
            }
            RenderCardEffect();
        }

        private void RenderCardEffect()
        {
            var basis=BoardCamera.transform;
            float flip=Mathf.SmoothStep(0,1,cardElapsed/.30f);
            cardCaster.rotation=basis.rotation*Quaternion.Euler(0,180*(1-flip),0);
            cardCaster.localScale=Vector3.one*(1.75f*PopScale(flip,.8f));
            cardCaster.gameObject.SetActive(cardElapsed<1.03f);
            SetCardLight(casterGlow,casterPosition+basis.forward*.05f,basis.rotation,
                Vector2.one*Mathf.Lerp(1.2f,1.5f,flip),GoldLight,.6f*(1-flip));

            for (int i=0;i<cardTargets.Count;i++)
            {
                var target=cardTargets[i];
                float age=cardElapsed-FirstBeam-i*Stagger;
                float glow=Mathf.Clamp01((age+.22f)/.20f);
                SetCardLight(target.Glow,target.Position,basis.rotation,
                    Vector2.one*Mathf.Lerp(1.1f,1.3f,glow),GoldLight,
                    age<Impact && cardElapsed>.06f ? Mathf.Lerp(.12f,.52f,glow) : 0);
                float beam=Mathf.Clamp01(age/BeamDuration);
                Vector3 delta=target.Position-casterPosition;
                float angle=Mathf.Atan2(Vector3.Dot(delta,basis.up),Vector3.Dot(delta,basis.right))*Mathf.Rad2Deg-90;
                float length=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/Impact));
                SetCardLight(target.Beam,casterPosition+delta*length*.5f,
                    basis.rotation*Quaternion.Euler(0,0,angle),
                    new Vector2(.15f*(1-beam*beam),delta.magnitude*length),
                    new Color(.62f,.91f,1),age>0 && delta.sqrMagnitude>.001f ? 1-beam*beam : 0);
                float flashAge=age-Impact;
                if (flashAge>=0 && !target.Replaced)
                {
                    target.Replaced=true;
                    if (target.Old?.Transform) target.Old.Transform.gameObject.SetActive(false);
                    if (target.Old?.Contact) target.Old.Contact.gameObject.SetActive(false);
                    target.Card.gameObject.SetActive(true);
                }
                if (target.Replaced)
                    target.Card.localScale=Vector3.one*PopScale(Mathf.Clamp01(flashAge/.18f),.7f);
                float flash=Mathf.Clamp01(flashAge/.12f);
                SetCardLight(target.Flash,target.Position,basis.rotation,
                    Vector2.one*Mathf.Lerp(.5f,1.3f,flash),GoldLight,flashAge>=0 ? 1-flash : 0);
            }

            foreach (var spark in cardSparks)
            {
                float age=cardElapsed-spark.Starts;
                float life=Mathf.Clamp01(age/spark.Duration);
                float size=spark.Size*(1-life);
                SetCardLight(spark.Light,spark.Origin+spark.Velocity*Mathf.Max(0,age)*.62f,
                    basis.rotation*Quaternion.Euler(0,0,spark.Angle+age*45),Vector2.one*size,
                    Color.Lerp(Color.white,GoldLight,life),age>=0 ? .68f*(1-life) : 0);
            }
        }

        private static float PopScale(float t,float start)
        {
            return t<.63f ? Mathf.SmoothStep(start,1.1f,t/.63f) : Mathf.SmoothStep(1.1f,1,(t-.63f)/.37f);
        }

        private void SetCardLight(Renderer renderer,Vector3 position,Quaternion rotation,Vector2 size,Color tint,float alpha)
        {
            renderer.enabled=alpha>.002f;
            if (!renderer.enabled) return;
            renderer.transform.SetPositionAndRotation(position,rotation);
            renderer.transform.localScale=new Vector3(size.x,size.y,1);
            tint.a=alpha;
            cardLightBlock.SetColor(LightColor,tint);
            renderer.SetPropertyBlock(cardLightBlock);
        }

        private void EndCardEffect(bool reconcile)
        {
            var board=cardBoard;
            cardBoard=null;
            foreach (var target in cardTargets)
            {
                if (target.Old?.Transform) target.Old.Transform.gameObject.SetActive(true);
                if (target.Old?.Contact) target.Old.Contact.gameObject.SetActive(true);
            }
            cardTargets.Clear();
            cardSparks.Clear();
            if (cardFxRoot) cardFxRoot.SetActive(false);
            if (Layers) Layers.SetModal(false);
            this.SendCommand(new CompleteSortCardTransformationCommand(board));
            if (!reconcile || !ready) return;
            ReconcilePieces();
            RefreshBoosterUi(true);
        }
    }
}
