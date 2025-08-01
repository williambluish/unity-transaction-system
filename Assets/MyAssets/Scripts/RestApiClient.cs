using com.vrmmorpg.entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace com.vrmmorpg.api {
    public class RestApiClient : MonoBehaviour
    {
        // singleton for easier access
        public static RestApiClient singleton;
        // public delegate int HttpResponse<T>(T body);
        public delegate void HttpResponse<T>(T body); // HTTP response, including a typed response body (which may be `null` if one was not returned)
        // api url
        public string serverUrl;
        // apis
        private const string AccountApiName = "accounts";
        private const string ItemApiName = "items";
        private const string PlayerCharacterApiName = "player_characters";

        [Serializable]
        class DeleteResponseMessage
        {
            public string id;
            //public string object; //TODO: rename variable
            public bool is_deleted;
        }

        [Serializable]
        class PlayerCharacter
        {
            public string id;
            public string name;
            public int level;
            public int health;
            public int experience;
            public string account_id;
            public float x;
            public float y;
            public float z;
        }

        [Serializable]
        class PlayerCharacterGroup
        {
            public PlayerCharacter[] group; // The variable name must be: group. Because of the GetAllRequest IEnumerator return value.
        }

        [Serializable]
        class AccountDto
        {
            public string id;
            public string username;
            public string first_name;
            public string last_name;
            public string email;
            public string password;
        }

        [Serializable]
        class AccountGroup
        {
            public AccountDto[] group; // The variable name must be: group. Because of the GetAllRequest IEnumerator return value.
        }

        [Serializable]
        class ItemDto
        {
            public string id;
            public string name;
            public int level;
            public int durability;
            public string player_character_id;
        }

        [Serializable]
        class ItemGroup
        {
            public ItemDto[] group; // The variable name must be: group. Because of the GetAllRequest IEnumerator return value.
        }

        // Use Awake to initialize variables or states before the application starts
        void Awake()
        {
            // initialize singleton
            if (singleton == null) singleton = this;
        }

        // Start is called before the first frame update
        void Start()
        {
            /*
            StartCoroutine(GetPlayerCharacter("b7810c13-f634-4420-9c0b-3cce0151285a"));
            StartCoroutine(GetAllPlayerCharacters());
            StartCoroutine(GetAllPlayerCharactersByAccountId("90fdbf9c-167f-4228-a081-73c62fbfac9e"));
            StartCoroutine(CreatePlayerCharacter());
            StartCoroutine(UpdatePlayerCharacter("a764e5b1-ed6e-4544-84b4-1e996b7ea87c"));
            StartCoroutine(DeletePlayerCharacter("e1233185-d46f-4b96-912e-099d124c0250"));
            */
            /*
            StartCoroutine(GetAccount("50f622a6-36b0-40ad-870a-559c87950a75"));
            StartCoroutine(GetAllAccounts());
            StartCoroutine(CreateAccount());
            StartCoroutine(UpdateAccount("88d69a1c-31f9-45e2-abb9-19a4ca251ad7"));
            StartCoroutine(DeleteAccount("853656bc-1128-4af9-ac01-d9654a55d2e9"));
            */
            //StartCoroutine(GetItem("27f66b76-5642-49dc-8455-d495ea1ff1f4"));
            //StartCoroutine(GetAllItems());
            //StartCoroutine(CreateItem());
            //StartCoroutine(UpdateItem("cb99f0fb-7e9d-4991-8612-c0e3eab81b15"));
            //StartCoroutine(DeleteItem("621ce1d6-54d7-4c2d-bbde-f6c9cda15997"));
        }

        // Update is called once per frame
        void Update()
        {

        }

        public IEnumerator CharacterLoad(string characterId, Player prefab, HttpResponse<GameObject> callback) {
            //StartCoroutine(GetPlayerCharacter(characterId));

            yield return GetPlayerCharacter(characterId, (playerCharacter) => {
                GameObject go = Instantiate(prefab.gameObject);
                Player player = go.GetComponent<Player>();

                // set values
                player.id = playerCharacter.id; // player character id
                player.name = playerCharacter.name; // player character name
                player.level = playerCharacter.level;
                player.health = playerCharacter.health;
                player.experience = playerCharacter.experience;
                player.accountId = playerCharacter.account_id;
                Vector3 position = new Vector3(playerCharacter.x, playerCharacter.y, playerCharacter.z);
                player.gameObject.transform.position = position;

                
                StartCoroutine(GetAllItemsByPlayerCharacterId(player.id, (items) => {
                    StartCoroutine(LoadItemStorage(player)); // Load Item Storage
                }));

                callback(go);
            });
        }

        public IEnumerator LoadItemStorage(Player player)
        {

            yield return GetAllItemsByPlayerCharacterId(player.id, (items) =>
            {
                foreach (ItemDto itemDto in items.group)
                {

                    if (ScriptableItem.dict.TryGetValue(itemDto.name.GetStableHashCode(), out ScriptableItem itemData))
                    {
                        Item item = new Item(itemData);

                        /*
                        public string id;
                        public string name;
                        public int level;
                        public int durability;
                        public string player_character_id;
                        */

                        item.id = itemDto.id;
                        //item.name = itemDto.name;
                        item.level = itemDto.level;
                        item.durability = itemDto.durability;
                        item.playerCharacterId = itemDto.player_character_id;

                        player.itemStorage.Add(item);
                    }
                    else Debug.LogWarning("LoadInventory: skipped item " + player.name + " for " + player.name + " because it doesn't exist anymore. If it wasn't removed intentionally then make sure it's in the Resources folder.");
                }

            });
        }

        #region Player Character
        // return a value from a coroutine
        // http://answers.unity.com/answers/1248539/view.html
        IEnumerator GetPlayerCharacter(string id, HttpResponse<PlayerCharacter> callback)
        {
            PlayerCharacter playerBody = new PlayerCharacter();

            // https://docs.microsoft.com/en-us/dotnet/csharp/how-to/concatenate-multiple-strings#string-interpolation
            // Request and wait for the desired player character body.
            yield return GetRequest<PlayerCharacter>($"{serverUrl}/{PlayerCharacterApiName}/{id}", (body) =>
            {
                playerBody = body;
                callback(body); // response body
            });

            Debug.Log($"Success:\nGetPlayerCharacter with id {id}: {JsonUtility.ToJson(playerBody)}");
        }

        IEnumerator GetAllPlayerCharacters()
        {
            PlayerCharacterGroup playerCharacterGroup = new PlayerCharacterGroup();

            // Request and wait for the desired player character body.
            yield return GetAllRequest<PlayerCharacterGroup>($"{serverUrl}/{PlayerCharacterApiName}", (body) =>
            {
                playerCharacterGroup = body;
            });
            Debug.Log("Success:\nGetAllPlayerCharacters: " + JsonUtility.ToJson(playerCharacterGroup));
        }

        IEnumerator GetAllPlayerCharactersByAccountId(string accountId)
        {
            PlayerCharacterGroup playerCharacterGroup = new PlayerCharacterGroup();

            // Request and wait for the desired player character body.
            yield return GetAllRequest<PlayerCharacterGroup>($"{serverUrl}/{PlayerCharacterApiName}?account_id={accountId}", (body) =>
            {
                playerCharacterGroup = body;
            });
            Debug.Log("Success:\nGetAllPlayerCharactersByAccountId: " + JsonUtility.ToJson(playerCharacterGroup));
        }

        IEnumerator CreatePlayerCharacter()
        {
            // Create object
            PlayerCharacter playerCharacter = new PlayerCharacter();

            // Set values
            playerCharacter.account_id = "90fdbf9c-167f-4228-a081-73c62fbfac9e";
            playerCharacter.name = "test8";
            playerCharacter.level = 2;
            playerCharacter.health = 90;
            playerCharacter.experience = 100;

            // Request and wait for the desired player character body.
            yield return PostRequest<PlayerCharacter>($"{serverUrl}/{PlayerCharacterApiName}", playerCharacter, (response) =>
            {
                playerCharacter = response;
            });
            Debug.Log("Success:\nCreatePlayerCharacter: " + JsonUtility.ToJson(playerCharacter));
        }

        IEnumerator UpdatePlayerCharacter(string playerCharacterId)
        {
            PlayerCharacter playerCharacter = new PlayerCharacter();
            playerCharacter.id = playerCharacterId;
            playerCharacter.name = "test4";
            playerCharacter.level = 2;
            playerCharacter.health = 1300;
            playerCharacter.experience = 870;

            // Request and wait for the desired player character body.
            yield return PutRequest<PlayerCharacter>($"{serverUrl}/{PlayerCharacterApiName}/{playerCharacterId}", playerCharacter, (response) =>
            {
                playerCharacter = response;
            });
            Debug.Log("Success:\nUpdatePlayerCharacter: " + JsonUtility.ToJson(playerCharacter));
        }

        IEnumerator DeletePlayerCharacter(string playerCharacterId)
        {
            DeleteResponseMessage deleteResponseMessage = new DeleteResponseMessage();

            // Request and wait for the desired player character body.
            yield return DeleteRequest<DeleteResponseMessage>($"{serverUrl}/{PlayerCharacterApiName}/{playerCharacterId}", (response) =>
            {
                deleteResponseMessage = response;
            });
            Debug.Log("Success:\nDeletePlayerCharacter: " + JsonUtility.ToJson(deleteResponseMessage));
        }
        #endregion

        #region Account
        IEnumerator GetAccount(string id)
        {
            AccountDto account = new AccountDto();

            // Request and wait for the desired body.
            yield return GetRequest<AccountDto>($"{serverUrl}/{AccountApiName}/{id}", (body) =>
            {
                account = body;
            });

            Debug.Log($"Success:\nGetAccount with id {id}: {JsonUtility.ToJson(account)}");
        }

        IEnumerator GetAllAccounts()
        {
            AccountGroup accountGroup = new AccountGroup();

            // Request and wait for the desired body.
            yield return GetAllRequest<AccountGroup>($"{serverUrl}/{AccountApiName}", (body) =>
            {
                accountGroup = body;
            });
            Debug.Log("Success:\nGetAllAccounts: " + JsonUtility.ToJson(accountGroup));
        }

        IEnumerator CreateAccount()
        {
            // Create object
            AccountDto account = new AccountDto();

            // Set values
            account.username = "testaccount1";
            account.first_name = "testy";
            account.last_name = "test";
            account.email = "test1@mail";
            account.password = "12345678";

            // Request and wait for the desired body.
            yield return PostRequest<AccountDto>($"{serverUrl}/{AccountApiName}", account, (response) =>
            {
                account = response;
            });
            Debug.Log("Success:\nCreateAccount: " + JsonUtility.ToJson(account));
        }

        IEnumerator UpdateAccount(string accountId)
        {
            // Create object
            AccountDto account = new AccountDto();

            // Set values
            account.id = accountId;
            account.username = "testy1";
            account.first_name = "testy";
            account.last_name = "test";
            account.email = "test1@mail";
            account.password = "123456789";

            // Request and wait for the desired body.
            yield return PutRequest<AccountDto>($"{serverUrl}/{AccountApiName}/{accountId}", account, (response) =>
            {
                account = response;
            });
            Debug.Log("Success:\nUpdateAccount: " + JsonUtility.ToJson(account));
        }

        IEnumerator DeleteAccount(string accountId)
        {
            DeleteResponseMessage deleteResponseMessage = new DeleteResponseMessage();

            // Request and wait for the desired body.
            yield return DeleteRequest<DeleteResponseMessage>($"{serverUrl}/{AccountApiName}/{accountId}", (response) =>
            {
                deleteResponseMessage = response;
            });
            Debug.Log("Success:\nDeleteAccount: " + JsonUtility.ToJson(deleteResponseMessage));
        }
        #endregion

        #region Item
        IEnumerator GetItem(string id)
        {
            ItemDto item = new ItemDto();

            // Request and wait for the desired body.
            yield return GetRequest<ItemDto>($"{serverUrl}/{ItemApiName}/{id}", (body) =>
            {
                item = body;
            });

            Debug.Log($"Success:\nGetItem with id {id}: {JsonUtility.ToJson(item)}");
        }

        IEnumerator GetAllItems()
        {
            ItemGroup itemGroup = new ItemGroup();

            // Request and wait for the desired body.
            yield return GetAllRequest<ItemGroup>($"{serverUrl}/{ItemApiName}", (body) =>
            {
                itemGroup = body;
            });
            Debug.Log("Success:\nGetAllItems: " + JsonUtility.ToJson(itemGroup));
        }

        IEnumerator GetAllItemsByPlayerCharacterId(string id, HttpResponse<ItemGroup> callback)
        {
            ItemGroup itemGroup = new ItemGroup();

            // Request and wait for the desired body.
            yield return GetAllRequest<ItemGroup>($"{serverUrl}/{ItemApiName}?player_character_id={id}", (body) =>
            {
                itemGroup = body;
                callback(body); // response body
            });
            Debug.Log("Success:\nGetAllItemsByPlayerCharacterId: " + JsonUtility.ToJson(itemGroup));
        }

        IEnumerator CreateItem()
        {
            // Create object
            ItemDto item = new ItemDto();

            // Set values
            item.player_character_id = "a9293f52-9937-4531-a424-833793c1f3eb";
            item.name = "Green apple";
            item.level = 1;
            item.durability = 15;

            // Request and wait for the desired body.
            yield return PostRequest<ItemDto>($"{serverUrl}/{ItemApiName}", item, (response) =>
            {
                item = response;
            });
            Debug.Log("Success:\nCreateItem: " + JsonUtility.ToJson(item));
        }

        IEnumerator UpdateItem(string id)
        {
            // Create object
            ItemDto item = new ItemDto();

            // Set values
            //item.player_character_id = "a9293f52-9937-4531-a424-833793c1f3eb";
            //item.player_character_id = "a5847585-f634-4f52-97b1-3bdbc349b493";
            item.name = "Green apple";
            item.level = 2;
            item.durability = 17;

            // Request and wait for the desired body.
            yield return PutRequest<ItemDto>($"{serverUrl}/{ItemApiName}/{id}", item, (response) =>
            {
                item = response;
            });
            Debug.Log("Success:\nUpdateItem: " + JsonUtility.ToJson(item));
        }

        IEnumerator DeleteItem(string id)
        {
            DeleteResponseMessage deleteResponseMessage = new DeleteResponseMessage();

            // Request and wait for the desired body.
            yield return DeleteRequest<DeleteResponseMessage>($"{serverUrl}/{ItemApiName}/{id}", (response) =>
            {
                deleteResponseMessage = response;
            });
            Debug.Log("Success:\nDeleteItem: " + JsonUtility.ToJson(deleteResponseMessage));
        }
        #endregion

        #region Http methods
        private enum RequestType
        {
            GET = 0,
            POST = 1,
            PUT = 2
        }
        // https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequest.Get.html
        // https://docs.unity3d.com/2020.3/Documentation/Manual/Coroutines.html
        // It�s best to use coroutines if you need to deal with long asynchronous operations, such as waiting for HTTP transfers,
        // asset loads, or file I/O to complete.
        private IEnumerator GetRequest<T>(string uri, HttpResponse<T> callback)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                string[] pages = uri.Split('/');
                int page = pages.Length - 1;

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError($"{pages[page]}: Error: {webRequest.error} \nuri: {uri}");
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError($"{pages[page]}: HTTP Error: {webRequest.error} \nuri: {uri}");
                        break;
                    case UnityWebRequest.Result.Success:
                        // https://docs.unity3d.com/Manual/JSONSerialization.html
                        callback(JsonUtility.FromJson<T>(webRequest.downloadHandler.text)); // response body
                        break;
                }
            }
        }
        private IEnumerator GetAllRequest<T>(string uri, HttpResponse<T> callback)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                string[] pages = uri.Split('/');
                int page = pages.Length - 1;

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError($"{pages[page]}: Error: {webRequest.error} \nuri: {uri}");
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError($"{pages[page]}: HTTP Error: {webRequest.error} \nuri: {uri}");
                        break;
                    case UnityWebRequest.Result.Success:
                        // https://docs.unity3d.com/Manual/JSONSerialization.html
                        //JSON must represent an object type
                        //http://answers.unity.com/answers/1503085/view.html
                        callback(JsonUtility.FromJson<T>($"{{\"group\":{webRequest.downloadHandler.text}}}")); // response body
                        break;
                }
            }
        }
        /// <summary>
        /// Keep in mind that the Content-Type header will be set to application/json by default.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="body"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        private IEnumerator PostRequest<T>(string url, T body, HttpResponse<T> callback)
        {
            // [DONE] I want to get to: new UnityWebRequest(url, "POST", new DownloadHandlerBuffer(), new UploadHandlerRaw(bodyRawData));
            using (UnityWebRequest webRequest = new UnityWebRequest(url, RequestType.POST.ToString()))
            {
                if (body != null)
                {
                    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(body));
                    webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw); // add json raw content
                }
                webRequest.downloadHandler = new DownloadHandlerBuffer(); // add download handler buffer
                webRequest.SetRequestHeader("Content-Type", "application/json"); // json raw header config
                yield return webRequest.SendWebRequest(); // send the request

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Error: {webRequest.error} \nurl: {url}");
                }
                else
                {
                    callback(JsonUtility.FromJson<T>(webRequest.downloadHandler.text));
                }
            }
        }
        /// <summary>
        /// Keep in mind that the Content-Type header will be set to application/json by default.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="body"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        private IEnumerator PutRequest<T>(string url, T body, HttpResponse<T> callback)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Put(url, JsonUtility.ToJson(body)))
            {
                webRequest.SetRequestHeader("Content-Type", "application/json"); // json raw header config
                yield return webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Error: {webRequest.error} \nurl: {url}");
                }
                else
                {
                    callback(JsonUtility.FromJson<T>(webRequest.downloadHandler.text));
                }
            }
        }
        private IEnumerator DeleteRequest<T>(string url, HttpResponse<T> callback)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Delete(url))
            {
                webRequest.downloadHandler = new DownloadHandlerBuffer(); // download server response
                yield return webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Error: {webRequest.error} \nurl: {url}");
                }
                else
                {
                    callback(JsonUtility.FromJson<T>(webRequest.downloadHandler.text));
                }
            }
        }
        #endregion
    }
}
