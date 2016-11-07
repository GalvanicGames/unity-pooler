# Unity Pooler
=======================
Object pooler for GameObjects and regular C# classes.

####Obtain!####
[Releases](https://github.com/GalvanicGames/unity-pooler/releases)

If you'd like the most up to date version (which is the most cool), then pull the repo or download it [here](https://github.com/GalvanicGames/unity-pooler/archive/master.zip) and copy the files in Assets to your project's Assets folder.

## Setup

Once the Unity Pooler asset has been imported into the project then the GameObject pool and the C# class pool is ready to be used.

## GameObject Pool 

To use Unity Pooler for prefabs the PoolableGameObject component must be on the prefab that will be pooled.

### PoolableGameObject Inspector ###

**Persist Across Scenes** - Should the pooled object not be destroyed when a new scene is loaded. This is useful if a game populates the entire pool upfront and keeps it throughout the game.

**Release Objects on Scene Transition** - If an object persists across scenes then should it be released back to the pool when the scene changes?

**Use Cap?** - Should the pool be capped at a certain count? If capped and the cap is hit then GameObjects will be recycled and reused to allow getting a new one.

**Cap Amount** - The cap count size.

### Populating ###

The GameObject pool will create new objects as it needs to but it's more efficient (and generates less garbase) if the pool can be prepopulated at the start of the Scene.

```csharp
public GameObject myGameObjectPrefab;

void Start()
{
  // Populdate pool up to a specified count. Will not go over that count if pool already has enough.
  myGameObjectPrefab.PopulatePool(10);
  
  // Add to the pool. Adds to the current pool count.
  myGameObjectPrefab.AddToPool(5);
  
  // Similar to adding but just one
  myGameObjectPrefab.IncrementPool();
  
  // If the Desired Population is set in the inspector then PopulateToDesired can be used.
  myGameObjectPrefab.PopulateToDesired();
}
```

Components that impelment the IGameObjectPoolable interface will have their member 'OnObjectCreated' invoked.

```csharp
public class MyComponent : MonoBehaviour, IGameObjectPoolable
{
  public void OnObjectCreated()
  {
  		Debug.Log("I was just created!");
	}
	
	public void OnObjectReused()
	{
	}
}
```

### Get ###

Get grabs a GameObject from the object pool. If will first grab a previously created object otherwise will create a new one if the cap hasn't been hit (or if the cap isn't enabled). 

```csharp
public GameObject myGameObjectPrefab;
private GameObject myGameObject;

void GetObjectFromPool()
{
  myGameObject = myGameObjectPrefab.Get();
  
  // I now have an instance of the myGameObjectPrefab. If this wasn't a reuse due to cap
  // being hit then the OnEnable will be called!
}
```

### Reuse ###

If caps are enabled and the cap is hit then Unity Pooler will take an active object and 'reuse' it. This basically means that it will return the previously active GameObject as the new GameObject. Components that implement IGameObjectPoolable will have their member 'OnOjectReused' invoked.

```csharp
public class MyComponent : MonoBehaviour, IGameObjectPoolable
{
  public void OnObjectCreated()
  {
  }
	
  public void OnObjectReused()
  {
    Debug.Log("I was just reused!");
  }
}
```

### Release ###

When an active GameObject is no longer needed then it needs to be released back to the object pool. **NOTE** failing to release an object results in leaked memory as the object pool will keep generating new objects.

```csharp
public GameObject myGameObjectPrefab;
private GameObject myGameObject;

void ReleaseObjBackToPool()
{
  myGameObject.Release();
  myGameObject = null;
  
  // I have released it back to the pool! It's OnDisable function will be invoked.
}
```

### Clearing ###

Generally a pool will be cleared when a scene transitions. The live and pooled objects will be destroyed when the scene is teared down. However, if objects are told to persist across scenes then this will not occur. In many cases there may not be a desire to clear a pool ever and the objects can just live on throughout the duration of the game. However, if it is desired to clear the pool then gameObject.ReleaseAndClearPool() can be called.

```csharp
public GameObject myGameObjectPrefab;

void ClearPool()
{
  myGameObjectPrefab.ReleaseAndClearPool();
  
  // Now all objects that were live have been released and all the objects that were in the
  // pool have been destroyed. The pool population is now zero.
}
```

## C# Class Pool

The C# class pool works very similar to the GameObject pool in functionality. Main difference is interaction with the pool is done through a static class.

### ObjectPool<T> fields ###

Setting fields to the class object pool is done through code on the ObjectPool<T> class.

```csharp
object[] constructorArgs
```

The constructor arguments that should be used when creating new objects. This should be set before any objects are created. If this isn't set (or set to null) then the default constructor is used.

```csharp
bool useCap
```

Should the class pool have a cap. If cap is enabled then active objects will be reused. 

```csharp
int capAmount
```

If capped, the amount the pool is capped to.

```csharp
bool useDictionaryCache
```

If cap is enabled then Release becomes more expensive as the class object pool will need to remove the active object from the object list. Enabling this will use a dictionary for instant look up. If the list is small then traversing the list will be cheaper than the dictionary look up. But if big enough then the dictionary becomes more efficient.

### IPoolable Interface ###

The class object pool works with any class but those that implement the IPoolable interface will have functions invoked on creation, get, release, and reuse.

```csharp
/// <summary>
/// Classes that implement IPoolable will receive calls from the ObjectPool.
/// </summary>
public interface IPoolable
{
	/// <summary>
	/// Invoked when the object is instantiated.
	/// </summary>
	void OnPoolCreate();

	/// <summary>
	/// Invoked when the object is grabbed from the object pool.
	/// </summary>
	void OnPoolGet();

	/// <summary>
	/// Invoked when the object is released back to the object pool.
	/// </summary>
	void OnPoolRelease();

	/// <summary>
	/// Invoked when the object is reused.
	/// </summary>
	void OnPoolReuse();
}
```
	
### Populating ###
	
Similarly to the GameObject pool, populating is done to create objects in the pool that are ready to be grabbed. The class object pooler will also create new objects on the fly as it needs to but this will generate garbage.

```csharp
public class MyClass : IPoolable // IPoolable isn't needed though!
{
  public void OnPoolCreate()
  {
  }
  
  public void OnPoolGet()
  {
  }
  
  public void OnPoolRelease()
  {
  }
  
  public void OnPoolReuse()
  {
  }
}

void Start()
{
  ObjectPool<MyClass>.useCap = true;
  ObjectPool<MyClass>.capAmount = 50;

  // Populates the pool up to a certain count. Will not go over that count if it already exists.
  ObjectPool<MyClass>.PopulatePool(40);
  
  // Adds to the current number in the pool.
  ObjectPool<MyClass>.AddToPool(9);
  
  // Adds one to the current number in the pool.
  ObjectPool<MyClass>.IncrementPool();
  
  // Each created MyClass instance will have its OnPoolCreate invoked!
}
```

### Get ###

Get retrieves an object from the class object pool. If no object exists then one will be created using the specified contructor arguments. If the cap is enabled and has been hit then an active object will be reused.

```csharp
private MyClass myClassObj;

void GetMyClass()
{
  myClassObj = ObjectPool<MyClass>.Get();
  
  // Since MyClass impelements IPoolable, OnPoolGet will be invoked. If the cap was hit then OnPoolReuse will be invoked instead.
}
```

### Release ###

Release places an object back into the class object pool. Note, if cap is enabled then this can be a more expensive operation.

```csharp
private MyClass myClassObj;

void ReleaseObj()
{
  ObjectPool<MyClass>.Release(myClassObj);
  myClassObj = null;
  
  // Since MyClass implements IPoolable, OnPoolRelease will be invoked!
}
```

### Clear ###

Since ObjectPool is a static class, it doesn't get wiped between scenes. It also makes no assumptions on scene transitions. If careful this can be fine (by using PopulatePool instead of AddToPool) but the ObjectPool can also be force wiped.

```csharp

void ClearObjectPool()
{
  ObjectPool<MyClass>.Clear(); // Now all the stored objects will be released to be garbage collected.
}
```

### Working with [Unity Game Loader](https://github.com/GalvanicGames/unity-game-loader) ###

The Unity Game Loader works by taking enumerators and progressing them as it needs to across frames. To make this work well with the object pooler two new population functions have been added.

```csharp
static IEnumerator PopulatePoolCo(this GameObject obj, int amount)
```

```csharp
static IEnumerator PopulatePoolCo(int numToPopulate)
```
