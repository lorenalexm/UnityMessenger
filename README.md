# Messenger
A delegate-based event messenger system built for [Unity](https://unity.com). This work is a direct rewrite of [CSharpMessenger Extended](https://wiki.unity3d.com/index.php/CSharpMessenger_Extended) which was posted to the Unify Community Wiki back in its day. The initial work was authored by Magnus Wolffelt, with Rod Hyde quotations, and Julie Iaccarino.

## Usage
The following code samples are designed to be ran within a [MonoBehaviour](https://docs.unity3d.com/ScriptReference/MonoBehaviour.html) derived class; as can be seen with the use of Unity reserved method names.

### Adding an event listener
Each listener must be registered within the class `OnEnable` method. For these examples, we are assuming one argument will be sent with each broadcast.
```csharp
private void OnEnable() 
{
    Messenger<float>.AddListener("PlayerDamaged", OnPlayerDamaged);
}
```

### Removing an event listener
In like, each listener must be cleaned up and removed within the class `OnDisable` method.
```csharp
private void OnDisable()
{
    Messenger<float>.RemoveListener("PlayerDamaged", OnPlayerDamaged);
}
```

### Writing your event listener
There are no rules imposed by the messenger on how the method must be writen, save for the signature must match what was registered.
```csharp
private void OnPlayerDamaged(float amount)
{
    PlayerHealth -= amount;
}
```

### Broadcasting to your listeners
When calling the Broadcast function, you again must make sure that signatures match what was registered.
```csharp
private void Attack()
{
    [...]
    Messenger<float>.Broadcast("PlayerDamaged", 5f);
}
```

Additionally the messenger will default to broadcasting using `MessengerMode.REQUIRE_LISTENER` mode. This is as it sounds, and requires that any event broadcast has at least one listener. A `BroadcastException` will be thrown otherwise. This can be circumvented by specifying the mode in your `Broadcast` call.
```csharp
private void Attack()
{
    [...]
    Messenger<float>.Broadcast("PlayerDamaged", 5f, MessengerMode.NO_LISTENER);
}
```