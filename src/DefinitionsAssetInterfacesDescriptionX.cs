using AasxIntegrationBase;
using AdminShellNS;
using Extensions;
using System;
using System.Collections.Generic;
using Aas = AasCore.Aas3_0;
using AasxPredefinedConcepts;

namespace AasxPredefinedConcepts.AssetInterfacesDescription
{

    [AasConcept("https://admin-shell.io/idta/AssetInterfacesDescription/1/0/Interface")]
    public class CD_GenericInterface
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#title", Card = AasxPredefinedCardinality.One)]
        string Title;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#created", Card = AasDefinition.Cardinality.ZeroToOne)]
        DateTime? Created;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#modified", Card = AasDefinition.Cardinality.ZeroToOne)]
        DateTime? Modified;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#support", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Support;

        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/EndpointMetadata", Card = AasDefinition.Cardinality.One)]
        CD_EndpointMetadata EndpointMetadata = new CD_EndpointMetadata();

        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/InterfaceMetadata", Card = AasDefinition.Cardinality.One)]
        CD_InterfaceMetadata InterfaceMetadata = new CD_InterfaceMetadata();

        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/ExternalDescriptor", Card = AasDefinition.Cardinality.ZeroToOne)]
        CD_ExternalDescriptor ExternalDescriptor = null;
    }

    [AasConcept("https://admin-shell.io/idta/AssetInterfacesDescription/1/0/EndpointMetadata")]
    public class CD_EndpointMetadata
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#base", Card = AasDefinition.Cardinality.One)]
        string Base;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/hypermedia#forContentType", Card = AasDefinition.Cardinality.One)]
        string ContentType;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#hasSecurityConfiguration", Card = AasDefinition.Cardinality.One)]
        CD_Security Security = new CD_Security();

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#definesSecurityScheme", Card = AasDefinition.Cardinality.One)]
        CD_SecurityDefinitions SecurityDefinitions = new CD_SecurityDefinitions();
    }

    [AasConcept("https://www.w3.org/2019/wot/td#hasSecurityConfiguration")]
    public class CD_Security
    {
    }

    [AasConcept("https://www.w3.org/2019/wot/td#definesSecurityScheme")]
    public class CD_SecurityDefinitions
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#NoSecurityScheme", Card = AasDefinition.Cardinality.ZeroToOne)]
        CD_Nosec_sc Nosec_sc = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#AutoSecurityScheme", Card = AasDefinition.Cardinality.ZeroToOne)]
        CD_Auto_sc Auto_sc = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#BasicSecurityScheme", Card = AasDefinition.Cardinality.ZeroToOne)]
        CD_Basic_sc Basic_sc = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#ComboSecurityScheme", Card = AasDefinition.Cardinality.ZeroToOne)]
        CD_Combo_sc Combo_sc = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#APIKeySecurityScheme", Card = AasDefinition.Cardinality.ZeroToOne)]
        CD_Apikey_sc Apikey_sc = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#PSKSecurityScheme", Card = AasDefinition.Cardinality.ZeroToOne)]
        CD_Psk_sc Psk_sc = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#DigestSecurityScheme", Card = AasDefinition.Cardinality.ZeroToOne)]
        CD_Digest_sc Digest_sc = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#BearerSecurityScheme", Card = AasDefinition.Cardinality.ZeroToOne)]
        CD_Bearer_sc Bearer_sc = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#OAuth2SecurityScheme", Card = AasDefinition.Cardinality.ZeroToOne)]
        CD_Oauth2_sc Oauth2_sc = null;
    }

    [AasConcept("https://www.w3.org/2019/wot/security#NoSecurityScheme")]
    public class CD_Nosec_sc
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#definesSecurityScheme", Card = AasDefinition.Cardinality.One)]
        string Scheme;
    }

    [AasConcept("https://www.w3.org/2019/wot/security#AutoSecurityScheme")]
    public class CD_Auto_sc
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#definesSecurityScheme", Card = AasDefinition.Cardinality.One)]
        string Scheme;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#proxy", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Proxy;
    }

    [AasConcept("https://www.w3.org/2019/wot/security#BasicSecurityScheme")]
    public class CD_Basic_sc
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#definesSecurityScheme", Card = AasDefinition.Cardinality.One)]
        string Scheme;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#name", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Name;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#in", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? In;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#proxy", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Proxy;
    }

    [AasConcept("https://www.w3.org/2019/wot/security#ComboSecurityScheme")]
    public class CD_Combo_sc
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#definesSecurityScheme", Card = AasDefinition.Cardinality.One)]
        string Scheme;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#oneOf", Card = AasDefinition.Cardinality.One)]
        CD_OneOf OneOf = new CD_OneOf();

        [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#allOf", Card = AasDefinition.Cardinality.One)]
        CD_AllOf AllOf = new CD_AllOf();

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#proxy", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Proxy;
    }

    [AasConcept("https://www.w3.org/2019/wot/json-schema#oneOf")]
    public class CD_OneOf
    {
    }

    [AasConcept("https://www.w3.org/2019/wot/json-schema#allOf")]
    public class CD_AllOf
    {
    }

    [AasConcept("https://www.w3.org/2019/wot/security#APIKeySecurityScheme")]
    public class CD_Apikey_sc
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#definesSecurityScheme", Card = AasDefinition.Cardinality.One)]
        string Scheme;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#name", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Name;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#in", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? In;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#proxy", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Proxy;
    }

    [AasConcept("https://www.w3.org/2019/wot/security#PSKSecurityScheme")]
    public class CD_Psk_sc
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#definesSecurityScheme", Card = AasDefinition.Cardinality.One)]
        string Scheme;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#identity", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Identity;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#proxy", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Proxy;
    }

    [AasConcept("https://www.w3.org/2019/wot/security#DigestSecurityScheme")]
    public class CD_Digest_sc
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#definesSecurityScheme", Card = AasDefinition.Cardinality.One)]
        string Scheme;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#name", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Name;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#in", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? In;

        string? Qop;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#proxy", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Proxy;

        string? Qop;

        string? Qop;
    }

    [AasConcept("https://www.w3.org/2019/wot/security#BearerSecurityScheme")]
    public class CD_Bearer_sc
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#definesSecurityScheme", Card = AasDefinition.Cardinality.One)]
        string Scheme;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#name", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Name;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#in", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? In;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#authorization", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Authorization;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#alg", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Alg;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#format", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Format;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#proxy", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Proxy;
    }

    [AasConcept("https://www.w3.org/2019/wot/security#OAuth2SecurityScheme")]
    public class CD_Oauth2_sc
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#definesSecurityScheme", Card = AasDefinition.Cardinality.One)]
        string Scheme;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#token", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Token;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#refresh", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Refresh;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#authorization", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Authorization;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#scopes", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Scopes;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#flow", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Flow;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#proxy", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Proxy;
    }

    [AasConcept("https://admin-shell.io/idta/AssetInterfacesDescription/1/0/InterfaceMetadata")]
    public class CD_InterfaceMetadata
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#PropertyAffordance", Card = AasDefinition.Cardinality.ZeroToOne)]
        CD_Properties Properties = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#ActionAffordance", Card = AasDefinition.Cardinality.ZeroToOne)]
        CD_Actions Actions = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#EventAffordance", Card = AasDefinition.Cardinality.ZeroToOne)]
        CD_Events Events = null;
    }

    [AasConcept("https://www.w3.org/2019/wot/td#PropertyAffordance")]
    public class CD_Properties
    {
        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfaceDescription/1/0/PropertyDefinition", Card = AasDefinition.Cardinality.ZeroToMany)]
        List<CD_PropertyName> PropertyName = new List<CD_PropertyName>();
    }

    [AasConcept("https://admin-shell.io/idta/AssetInterfaceDescription/1/0/PropertyDefinition")]
    public class CD_PropertyName
    {
        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/key", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Key;

        [AasConcept(Cd = "https://www.w3.org/1999/02/22-rdf-syntax-ns#type", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Type;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#title", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Title;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#isObservable", Card = AasDefinition.Cardinality.ZeroToOne)]
        bool? Observable;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#const", Card = AasDefinition.Cardinality.ZeroToOne)]
        int? Const;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#default", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Default;

        [AasConcept(Cd = "https://schema.org/unitCode", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Unit;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#items", Card = AasDefinition.Cardinality.ZeroToOne)]
        CD_Items Items = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#properties", Card = AasDefinition.Cardinality.ZeroToOne)]
        CD_Properties Properties = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#hasForm", Card = AasDefinition.Cardinality.One)]
        CD_Forms Forms = new CD_Forms();
    }

    [AasConcept("https://www.w3.org/2019/wot/json-schema#items")]
    public class CD_Items
    {
        [AasConcept(Cd = "https://www.w3.org/1999/02/22-rdf-syntax-ns#type", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Type;

        [AasConcept(Cd = "https://schema.org/unitCode", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Unit;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#default", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Default;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#const", Card = AasDefinition.Cardinality.ZeroToOne)]
        int? Const;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#isObservable", Card = AasDefinition.Cardinality.ZeroToOne)]
        bool? Observable;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#title", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Title;
    }

    [AasConcept("https://www.w3.org/2019/wot/json-schema#properties")]
    public class CD_Properties
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#propertyName", Card = AasDefinition.Cardinality.ZeroToMany)]
        List<CD__propertyName_> _propertyName_ = new List<CD__propertyName_>();

        [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#propertyName", Card = AasDefinition.Cardinality.ZeroToMany)]
        List<CD__propertyName_> PropertyName = new List<CD__propertyName_>();
    }

    [AasConcept("https://www.w3.org/2019/wot/json-schema#propertyName")]
    public class CD__propertyName_
    {
        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/key", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Key;

        [AasConcept(Cd = "https://www.w3.org/1999/02/22-rdf-syntax-ns#type", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Type;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#title", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Title;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#isObservable", Card = AasDefinition.Cardinality.ZeroToOne)]
        bool? Observable;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#const", Card = AasDefinition.Cardinality.ZeroToOne)]
        int? Const;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#default", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Default;

        [AasConcept(Cd = "https://schema.org/unitCode", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Unit;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#items", Card = AasDefinition.Cardinality.ZeroToOne)]
        CD_Items Items = null;
    }

    [AasConcept("https://www.w3.org/2019/wot/td#hasForm")]
    public class CD_Forms
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/hypermedia#hasTarget", Card = AasDefinition.Cardinality.One)]
        string Href;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/hypermedia#forContentType", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? ContentType;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#hasSecurityConfiguration", Card = AasDefinition.Cardinality.One)]
        CD_Security Security = new CD_Security();

        [AasConcept(Cd = "https://www.w3.org/2011/http#methodName", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Htv_methodName;

        [AasConcept(Cd = "https://www.w3.org/2011/http#headers", Card = AasDefinition.Cardinality.ZeroToOne)]
        CD_Htv_headers Htv_headers = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/modbus#Function", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Modbus_function;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/modbus#Entity", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Modbus_entity;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/modbus#hasZeroBasedAddressingFlag", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Modbus_zeroBasedAddressing;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/modbus#pollingTime", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Modbus_pollingTime;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/mqtt#hasQoSFlag", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Modbus_timeout;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/modbus#type", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Modbus_type;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/mqtt#hasRetainFlag", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Mqv_retain;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/mqtt#ControlPacket", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Mqv_controlPacket;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/mqtt#hasQoSFlag", Card = AasDefinition.Cardinality.ZeroToOne)]
        string? Mqv_qos;
    }

    [AasConcept("https://www.w3.org/2011/http#headers")]
    public class CD_Htv_headers
    {
        [AasConcept(Cd = "https://www.w3.org/2011/http#headers", Card = AasDefinition.Cardinality.OneToMany)]
        List<CD_Htv_headers> Htv_headers = new List<CD_Htv_headers>();

        [AasConcept(Cd = "https://www.w3.org/2011/http#fieldName", Card = AasDefinition.Cardinality.One)]
        string Htv_fieldName;

        [AasConcept(Cd = "https://www.w3.org/2011/http#fieldValue", Card = AasDefinition.Cardinality.One)]
        string Htv_fieldValue;
    }

    [AasConcept("https://www.w3.org/2019/wot/td#ActionAffordance")]
    public class CD_Actions
    {
    }

    [AasConcept("https://www.w3.org/2019/wot/td#EventAffordance")]
    public class CD_Events
    {
    }

    [AasConcept("https://admin-shell.io/idta/AssetInterfacesDescription/1/0/ExternalDescriptor")]
    public class CD_ExternalDescriptor
    {
    }

    [AasConcept("https://admin-shell.io/idta/AssetInterfacesDescription/1/0/Submodel")]
    public class CD_AssetInterfacesDescription
    {
        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/Interface", Card = AasDefinition.Cardinality.ZeroToMany)]
        List<CD_GenericInterface> InterfaceHTTP = new List<CD_GenericInterface>();

        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/Interface", Card = AasDefinition.Cardinality.ZeroToMany)]
        List<CD_GenericInterface> InterfaceMODBUS = new List<CD_GenericInterface>();

        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/Interface", Card = AasDefinition.Cardinality.ZeroToMany)]
        List<CD_GenericInterface> InterfaceMQTT = new List<CD_GenericInterface>();
    }
}

