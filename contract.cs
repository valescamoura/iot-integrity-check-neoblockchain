using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.Numerics;

namespace IoTIntegrityCheck
{
    [ManifestExtra("Author", "Neo")]
    [ManifestExtra("Email", "dev@neo.org")]
    [ManifestExtra("Description", "This is a contract example")]
    public class IntegrityCheck : SmartContract
    {
        [InitialValue("NforeidHBjJDK6sGdxiAMRfQwW8UnkwMFm", ContractParameterType.Hash160)]
        static readonly UInt160 Owner = default;
        
        private static String OwnerStr = "NforeidHBjJDK6sGdxiAMRfQwW8UnkwMFm";

        public static readonly int NumVector = 10; // valor fixado a fim de facilitar a implementação do protótipo
        public static String[] AdministrativeEntities = new String[NumVector];
        public static String[] CommonEntities = new String[NumVector];

        private static bool IsOwner() => Runtime.CheckWitness(Owner);

        private static String SerializeVector(String[] vector)
        {
            String SerializedVector = ""
            foreach (String v in vector)
            {
                SerializedVector += v + "-";
            }
            return SerializedVector;
        }

        private static String[] DeserializeVector(String string)
        {
            return string.Split( '-' );
        }

        private static String[] GetAdministrativeEntities()
        {
            return DeserializeVector(Storage.Get(Storage.CurrentContext, "EAs"));
        }

        private static String[] GetCommonEntities()
        {
            return DeserializeVector(Storage.Get(Storage.CurrentContext, "ECs"));
        }

        private static void AddEA(String EntityId) 
        {
            String[] ActualAdministrativeEntities = GetAdministrativeEntities();
            String value = $"{SerializeVector(ActualAdministrativeEntities)}{EntityId}";
            Storage.Put(Storage.CurrentContext, "EAs", value);
        }

        private static void AddEC(String EntityId)
        {
            String[] ActualCommonEntities = GetCommonEntities();
            String value = $"{SerializeVector(ActualCommonEntities)}{EntityId}";
            Storage.Put(Storage.CurrentContext, "ECs", value);
        }

        private static void RemoveEA(String EntityId) 
        {
            String ActualAdministrativeEntities = Serialize(GetAdministrativeEntities());
            String NewAdministrativeEntities = ActualAdministrativeEntities.Replace($"-{EntityId}", "");
            String value = NewAdministrativeEntities;
            Storage.Put(Storage.CurrentContext, "EAs", value);
        }

        private static void RemoveEC(String EntityId)
        {
            String ActualCommonEntities = Serialize(GetCommonEntities());
            String NewCommonEntities = ActualCommonEntities.Replace($"-{EntityId}", "");
            String value = NewCommonEntities;
            Storage.Put(Storage.CurrentContext, "ECs", value);
        }

        private static bool IsAdministrativeEntity(String id) 
        {
            AdministrativeEntities = GetAdministrativeEntities();
            foreach(UInt160 AdministrativeEntity in AdministrativeEntities)
            {
                // FIXME: o vetor deveria ser do tipo UInt160 para utilização do CheckWitness
                // if(Runtime.CheckWitness(AdministrativeEntity)) {
                //     return true;
                // }

                if(id == AdministrativeEntity)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsCommonEntity(String id) 
        {
            CommonEntities = GetCommonEntities();
            foreach(UInt160 CommonEntity in CommonEntities)
            {
                // FIXME
                // if(Runtime.CheckWitness(CommonEntity)) {
                //     return true;
                // }

                if(id == CommonEntity)
                {
                    return true;
                }
            }
            return false;
        }

        // When this contract address is included in the transaction signature,
        // this method will be triggered as a VerificationTrigger to verify that the signature is correct.
        // For example, this method needs to be called when withdrawing token from the contract.
        public static bool Verify() => IsOwner();

        public static bool VerifyEntity(String EntityId, String WitnessId = "") 
        {            
            if(IsOwner() | IsAdministrativeEntity(WitnessId) | IsCommonEntity(WitnessId)) // checkWitness
            {
                if(IsAdministrativeEntity(EntityId) | IsCommonEntity(EntityId) | EntityId == OwnerStr) 
                {
                    return True;
                }
            }
            return false;
        }

        public static string CreateEntity(String EntityType, String EntityWalletAddress, String SubjectInfo, String WitnessId = "MASTER")
        {
            if(IsOwner() | IsAdministrativeEntity(WitnessId)) {
                String ParentId = WitnessId;
                String key = EntityWalletAddress;
                String value = $"{SubjectInfo}-{ParentId}" // utilizando strings pois nao foi possível persistir outro tipo de dado
                Storage.Put(Storage.CurrentContext, key, value);

                if(EntityType == "EA")
                {
                    AddEA(EntityWalletAddress);
                }
                else
                {
                    AddEC(EntityWalletAddress);
                }
                return "Entidade cadastrada com sucesso.";
            }
            else {
                return "Nao foi possivel cadastrar a entidade, voce nao possui permissao.";
            }
        }

        public static string DeleteEntity(String EntityType, String EntityWalletAddress, String WitnessId = "MASTER")
        {
            if(IsOwner() | IsAdministrativeEntity(WitnessId)) {
                String key = EntityWalletAddress;
                Storage.Delete(Storage.CurrentContext, key);

                if(EntityType == "EA")
                {
                    RemoveEA(EntityWalletAddress);
                }
                else
                {
                    RemoveEC(EntityWalletAddress);
                }
                return "Entidade remover com sucesso.";
            }
            else {
                return "Nao foi possivel remover a entidade, voce nao possui permissao.";
            }
        }

        public static String[] GetEntities(String WitnessId = "MASTER")
        {
            if(IsOwner() | IsAdministrativeEntity(WitnessId)) {
                String eas = Storage.Get(Storage.CurrentContext, "EAs");
                String ecs = Storage.Get(Storage.CurrentContext, "ECs");
                String[] es = DeserializeVector($"{eas}-{ecs}");
                
                return es;
            }
            else {
                return ["Nao foi possivel remover a entidade, voce nao possui permissao."];
            }
        }

        public static String GetInfo(String EntityWalletAddress, String WitnessId = "MASTER")
        {
            if(IsOwner() | IsAdministrativeEntity(WitnessId)) {
                String[] info = DeserializeVector(Storage.Get(Storage.CurrentContext, EntityWalletAddress));
                String response = $"SubjectInfo: {info[0]} - ParentId: {info[1]}";
                
                return response;
            }
            else {
                return "Voce nao possui permissao para executar essa operacao.";
            }
        }

        // It will be executed during deploy
        public static void _deploy(object data, bool update)
        {
            if (update) return;
      
            Storage.Put(Storage.CurrentContext, "MASTER", OwnerStr);
            Storage.Put(Storage.CurrentContext, "EAs", SerializeVector(AdministrativeEntities));
            Storage.Put(Storage.CurrentContext, "ECs", SerializeVector(CommonEntities));
        }

        public static void Update(ByteString nefFile, string manifest)
        {
            if (!IsOwner()) throw new Exception("No authorization.");
            ContractManagement.Update(nefFile, manifest, null);
        }

        public static void Destroy()
        {
            if (!IsOwner()) throw new Exception("No authorization.");
            ContractManagement.Destroy();
        }
    }
}
