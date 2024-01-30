using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxPredefinedConcepts
{
    class AIDResources
    {   public static JObject EndpointMetadataJObject = JObject.Parse(
         @"
          {
		  'interface': {
                'AasElementType': 'SubmodelElementCollection',
                'multiplcity': '0..1',
                'description': 'An abstraction of a physical or a virtual entity whose metadata and interfaces are described by a WoT Thing Description, whereas a virtual entity is the composition of one or more Things.',
                'semanticReference': 'https://admin-shell.io/idta/AssetInterfacesDescription/1/0/Interface',
                'formtext': 'interface',
                'presetIdShort': 'interface{00:00}',
                'childs': [
				  {
                    'AasElementType': 'Property',
                    'valueType' : 'xs:string',
                    'formtext': 'title',
                    'presetIdShort': 'title',
                    'multiplcity': '1',
                    'description': 'Provides a human-readable title',
                    'semanticReference': 'https://www.w3.org/2019/wot/td#title'
                  },
				  {
                    'AasElementType': 'Property',
                    'valueType' : 'xs:string',
                    'formtext': 'created',
                    'presetIdShort': 'created',
                    'multiplcity': '0..1',
                    'description': 'Provides information when the TD instance was created.',
                    'semanticReference': 'http://purl.org/dc/terms/created'
                  },
				  {
                    'AasElementType': 'Property',
                    'valueType' : 'xs:string',
                    'formtext': 'modified',
                    'presetIdShort': 'modified',
                    'multiplcity': '0..1',
                    'description': 'Provides information when the TD instance was last modified.',
                    'semanticReference': 'http://purl.org/dc/terms/modified'
                  },
				  {
                    'AasElementType': 'Property',
                    'valueType' : 'xs:string',
                    'formtext': 'support',
                    'presetIdShort': 'support',
                    'multiplcity': '0..1',
                    'description': 'Provides information about the TD maintainer as URI scheme (e.g., mailto [RFC6068], tel [RFC3966], https [RFC9112]).',
                    'semanticReference': 'https://www.w3.org/2019/wot/td#supportContact'
                  },
				{
                'AasElementType': 'SubmodelElementCollection',
                'multiplcity': '0..1',
                'description': 'Provides the metadata of the asset’s endpoint (base, content type that is used for interaction, etc)',
                'semanticReference': 'https://admin-shell.io/idta/AssetInterfacesDescription/1/0/EndpointMetadata',
                'formtext': 'EndpointMetadata',
                'presetIdShort': 'EndpointMetadata',
                'childs': [
                  {
                    'AasElementType': 'Property',
                    'valueType' : 'xs:string',
                    'formtext': 'base',
                    'presetIdShort': 'base',
                    'multiplcity': '0..1',
                    'description': 'Define the base URI that is used for all relative URI references throughout a TD document. In TD instances: all relative URIs are resolved relative to the base URI using the algorithm defined in [RFC3986].',
                    'semanticReference': 'https://www.w3.org/2019/wot/td#baseURI'
                  },
                  {
                    'AasElementType': 'Property',
                    'valueType' : 'xs:string',
                    'formtext': 'contentType',
                    'presetIdShort': 'contentType',
                    'multiplcity': '0..1',
                    'description': 'Assign a content type based on a media type (e.g.: text/plain) and potential parameters (e.g.: charset=utf-8) for the media type [RFC2046].',
                    'semanticReference': 'https://www.w3.org/2019/wot/hypermedia#forContentType'
                  },
                  {
                    'AasElementType': 'SubmodelElementCollection',
                    'multiplcity': '0..1',
                    'description': 'Defines the security scheme according to W3C',
                    'semanticReference': 'https://www.w3.org/2019/wot/hypermedia#securityDefinitions',
                    'formtext': 'securityDefinitions',
                    'presetIdShort': 'securityDefinitions',
                    'childs': [
                      {
                        'AasElementType': 'SubmodelElementCollection',
                        'multiplcity': '0..1',
                        'description': 'Bearer Security Definition',
                        'semanticReference': 'https://www.w3.org/2019/wot/security#BearerSecurityScheme',
                        'formtext': 'bearer_sc',
                        'presetIdShort': 'bearer_sc',
                        'childs': [
                          {
                            'AasElementType': 'Property',
                            'valueType' : 'xs:anyURI',
                            'formtext': 'proxy',
                            'presetIdShort': 'proxy',
                            'multiplcity': '0..1',
                            'description': 'Identification of the security mechanism being configured.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#proxy'
                          },
                          {
                            'AasElementType': 'Property',
                            'valueType' : 'xs:string',
                            'formtext': 'scheme',
                            'presetIdShort': 'scheme',
                            'multiplcity': '1',
                            'description': 'URI of the proxy server this security configuration provides access to. If not given, the corresponding security configuration is for the endpoint.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#SecurityScheme'
                          },
                          {
                            'AasElementType': 'Property',
                            'valueType' : 'xs:anyURI',
                            'formtext': 'authorization',
                            'presetIdShort': 'authorization',
                            'multiplcity': '0..1',
                            'description': 'URI of the authorization server.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#authorization'
                          },
                          {
                            'AasElementType': 'Property',
                            'valueType' : 'xs:string',
                            'formtext': 'name',
                            'presetIdShort': 'name',
                            'multiplcity': '0..1',
                            'description': 'Name for query: header: cookie: or uri parameters.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#name'
                          },
                          {
                            'AasElementType': 'Property',
                            'valueType' : 'xs:string',
                            'formtext': 'alg',
                            'presetIdShort': 'alg',
                            'multiplcity': '0..1',
                            'description': 'Encoding: encryption: or digest algorithm.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#alg'
                          },
                          {
                            'AasElementType': 'Property',
                            'valueType' : 'xs:string',
                            'formtext': 'format',
                            'presetIdShort': 'format',
                            'multiplcity': '0..1',
                            'description': 'Specifies format of security authentication information.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#format'
                          },
                          {
                            'AasElementType': 'Property',
                            'valueType' : 'xs:string',
                            'formtext': 'in',
                            'presetIdShort': 'in',
                            'multiplcity': '0..1',
                            'description': 'Specifies the location of security authentication information.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#in'
                          }
                        ]
                      },
                      {
                        'AasElementType': 'SubmodelElementCollection',
                        'multiplcity': '0..1',
                        'description': 'Digest Security Definition',
                        'semanticReference': 'https://www.w3.org/2019/wot/security#DigestSecurityScheme',
                        'formtext': 'digest_sc',
                        'presetIdShort': 'digest_sc',
                        'childs': [
                          {
                            'AasElementType': 'Property',
                            'valueType' : 'xs:anyURI',
                            'formtext': 'proxy',
                            'presetIdShort': 'proxy',
                            'multiplcity': '0..1',
                            'description': 'Identification of the security mechanism being configured.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#proxy'
                          },
                          {
                            'AasElementType': 'Property',
							'valueType' : 'xs:string',							
                            'formtext': 'scheme',
                            'presetIdShort': 'scheme',
                            'multiplcity': '1',
                            'description': 'URI of the proxy server this security configuration provides access to. If not given, the corresponding security configuration is for the endpoint.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#SecurityScheme'
                          },
                          {
                            'AasElementType': 'Property',								
							'valueType' : 'xs:string',								
                            'formtext': 'name',
                            'presetIdShort': 'name',
                            'multiplcity': '0..1',
                            'description': 'Name for query: header: cookie: or uri parameters.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#name'
                          },
                          {
                            'AasElementType': 'Property',
							'valueType' : 'xs:string',							
                            'formtext': 'in',
                            'presetIdShort': 'in',
                            'multiplcity': '0..1',
                            'description': 'Specifies the location of security authentication information.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#in'
                          }
                        ]
                      },
                      {
                        'AasElementType': 'SubmodelElementCollection',
                        'multiplcity': '0..1',
                        'description': 'Api Key Security Definition',
                        'semanticReference': 'https://www.w3.org/2019/wot/security#APIKeySecurityScheme',
                        'formtext': 'apikey_sc',
                        'presetIdShort': 'apikey_sc',
                        'childs': [
                          {
                            'AasElementType': 'Property',
                            'valueType' : 'xs:anyURI',							
                            'formtext': 'proxy',
                            'presetIdShort': 'proxy',
                            'multiplcity': '0..1',
                            'description': 'Identification of the security mechanism being configured.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#proxy'
                          },
                          {
                            'AasElementType': 'Property',
							'valueType' : 'xs:string',							
                            'formtext': 'scheme',
                            'presetIdShort': 'scheme',
                            'multiplcity': '1',
                            'description': 'URI of the proxy server this security configuration provides access to. If not given, the corresponding security configuration is for the endpoint.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#SecurityScheme'
                          },
                          {
                            'AasElementType': 'Property',
							'valueType' : 'xs:string',							
                            'formtext': 'name',
                            'presetIdShort': 'name',
                            'multiplcity': '0..1',
                            'description': 'Name for query: header: cookie: or uri parameters.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#name'
                          },
                          {
                            'AasElementType': 'Property',
							'valueType' : 'xs:string',							
                            'formtext': 'in',
                            'presetIdShort': 'in',
                            'multiplcity': '0..1',
                            'description': 'Specifies the location of security authentication information.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#in'
                          }
                        ]
                      },
                      {
                        'AasElementType': 'SubmodelElementCollection',
                        'multiplcity': '0..1',
                        'description': 'PSK Security Definition',
                        'semanticReference': 'https://www.w3.org/2019/wot/security#PSKSecurityScheme',
                        'formtext': 'psk_sc',
                        'presetIdShort': 'psk_sc',
                        'childs': [
                          {
                            'AasElementType': 'Property',
                            'valueType' : 'xs:anyURI',
                            'formtext': 'proxy',
                            'presetIdShort': 'proxy',
                            'multiplcity': '0..1',
                            'description': 'Identification of the security mechanism being configured.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#proxy'
                          },
                          {
                            'AasElementType': 'Property',
							'valueType' : 'xs:string',
                            'formtext': 'scheme',
                            'presetIdShort': 'scheme',
                            'multiplcity': '1',
                            'description': 'URI of the proxy server this security configuration provides access to. If not given, the corresponding security configuration is for the endpoint.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#SecurityScheme'
                          },
                          {
                            'AasElementType': 'Property',
							'valueType' : 'xs:string',							
                            'formtext': 'identity',
                            'presetIdShort': 'identity',
                            'multiplcity': '0..1',
                            'description': 'Identifier providing information which can be used for selection or confirmation.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#identity'
                          }
                        ]
                      },
                      {
                        'AasElementType': 'SubmodelElementCollection',
                        'multiplcity': '0..1',
                        'description': 'Basic Security Definition',
                        'semanticReference': 'https://www.w3.org/2019/wot/security#BasicSecurityScheme',
                        'formtext': 'basic_sc',
                        'presetIdShort': 'basic_sc',
                        'childs': [
                          {
                            'AasElementType': 'Property',
                            'valueType' : 'xs:anyURI',
                            'formtext': 'proxy',
                            'presetIdShort': 'proxy',
                            'multiplcity': '0..1',
                            'description': 'Identification of the security mechanism being configured.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#proxy'
                          },
                          {
                            'AasElementType': 'Property',
							'valueType' : 'xs:string',
                            'formtext': 'scheme',
                            'presetIdShort': 'scheme',
                            'multiplcity': '1',
                            'description': 'URI of the proxy server this security configuration provides access to. If not given, the corresponding security configuration is for the endpoint.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#SecurityScheme'
                          },
                          {
                            'AasElementType': 'Property',
							'valueType' : 'xs:string',
                            'formtext': 'name',
                            'presetIdShort': 'name',
                            'multiplcity': '0..1',
                            'description': 'Name for query: header: cookie: or uri parameters.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#name'
                          },
                          {
                            'AasElementType': 'Property',
							'valueType' : 'xs:string',
                            'formtext': 'in',
                            'presetIdShort': 'in',
                            'multiplcity': '0..1',
                            'description': 'Name for query, header, cookie, or uri parameters',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#name'
                          }
                        ]
                      },
                      {
                        'AasElementType': 'SubmodelElementCollection',
                        'multiplcity': '0..1',
                        'description': 'OAuth2 Security Definition',
                        'semanticReference': 'https://www.w3.org/2019/wot/security#OAuth2SecurityScheme',
                        'formtext': 'oauth2_sc',
                        'presetIdShort': 'oauth2_sc',
                        'childs': [
                          {
                            'AasElementType': 'Property',
                            'valueType' : 'xs:anyURI',
                            'formtext': 'proxy',
                            'presetIdShort': 'proxy',
                            'multiplcity': '0..1',
                            'description': 'Identification of the security mechanism being configured.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#proxy'
                          },
                          {
                            'AasElementType': 'Property',
							'valueType' : 'xs:string',							
                            'formtext': 'scheme',
                            'presetIdShort': 'scheme',
                            'multiplcity': '1',
                            'description': 'URI of the proxy server this security configuration provides access to. If not given, the corresponding security configuration is for the endpoint.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#SecurityScheme'
                          },
                          {
                            'AasElementType': 'Property',
                            'valueType' : 'xs:anyURI',							
                            'formtext': 'authorization',
                            'presetIdShort': 'authorization',
                            'multiplcity': '0..1',
                            'description': 'URI of the authorization server.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#authorization'
                          },
                          {
                            'AasElementType': 'Property',
                            'valueType' : 'xs:anyURI',							
                            'formtext': 'token',
                            'presetIdShort': 'token',
                            'multiplcity': '0..1',
                            'description': 'URI of the token server.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#token'
                          },
                          {
                            'AasElementType': 'Property',
                            'valueType' : 'xs:anyURI',							
                            'formtext': 'refresh',
                            'presetIdShort': 'refresh',
                            'multiplcity': '0..1',
                            'description': 'URI of the refresh server.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#refresh'
                          },
                          {
                            'AasElementType': 'Property',
							'valueType' : 'xs:string',							
                            'formtext': 'format',
                            'presetIdShort': 'format',
                            'multiplcity': '0..1',
                            'description': 'Specifies format of security authentication information.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#format'
                          },
                          {
                            'AasElementType': 'Property',
							'valueType' : 'xs:string',
                            'formtext': 'flow',
                            'presetIdShort': 'flow',
                            'multiplcity': '0..1',
                            'description': 'Defines authorization flow such as code or client',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#flow'
                          },
                          {
                            'AasElementType': 'SubmodelElementCollection',
                            'formtext': 'scopes',
                            'presetIdShort': 'scopes',
                            'multiplcity': '0..1',
                            'description': 'Set of authorization scope identifiers provided as an array. These are provided in tokens returned by an authorization server and associated with forms in order to identify what resources a client may access and how.',
                            'semanticReference': 'https://www.w3.org/2019/wot/security#scopes',
                            'childs': [
                              {
                                'AasElementType': 'Property',
								'valueType' : 'xs:string',								
                                'formtext': 'scope',
                                'presetIdShort': 'scope{00:00}',
                                'multiplcity': '0..*',
                                'description': 'Authorization scope identifier',
                                'semanticReference': 'https://www.w3.org/2019/wot/security#scopes'
                              }
                            ]
                          }
                        ]
                      }
                    ]
                  },
                  {
                    'AasElementType': 'SubmodelElementCollection',
                    'multiplcity': '1',
                    'description': 'Selects one or more of the security scheme(s) that can be applied at runtime from the collection of security schemes defines in securityDefinitions',
                    'semanticReference': 'https://www.w3.org/2019/wot/td#hasSecurityConfiguration',
                    'formtext': 'security',
                    'presetIdShort': 'security',
                    'childs': [
                      {
                        'AasElementType': 'ReferenceElement',
                        'multiplcity': '0..*',
                        'description': 'Reference element to security scheme definition',
                        'semanticReference': 'https://www.w3.org/2019/wot/td#hasSecurityConfiguration',
                        'formtext': 'security',
                        'presetIdShort': ''
                      }
                    ]
                  },
                  {
                    'AasElementType': 'Property',
                    'valueType' : 'xs:boolean',					
                    'formtext': 'modv_mostSignificantByte',
                    'presetIdShort': 'modv_mostSignificantByte',
                    'multiplcity': '0..1',
                    'description': 'Define the base URI that is used for all relative URI references throughout a TD document. In TD instances: all relative URIs are resolved relative to the base URI using the algorithm defined in [RFC3986].',
                    'semanticReference': 'https://www.w3.org/2019/wot/modbus#hasMostSignificByte'
                  },
                  {
                    'AasElementType': 'Property',
                    'valueType' : 'xs:boolean',					
                    'formtext': 'modv_mostSignificantWord',
                    'presetIdShort': 'modv_mostSignificantWord',
                    'multiplcity': '0..1',
                    'description': 'When true: it describes that the word order of the data in the Modbus message is the most significant word first (i.e.: no word swapping). When false: it describes the least significant word first (i.e. word swapping) ',
                    'semanticReference': 'https://www.w3.org/2019/wot/modbus#hasMostSignificantWord'
                  }
                ]
              },
			  {
                'AasElementType': 'SubmodelElementCollection',
                'multiplcity': '0..1',
                'description': 'Provides an place for existing description files (e.g., Thing Description, GSDML, etc,).',
                'semanticReference': 'https://admin-shell.io/idta/AssetInterfacesDescription/1/0/ExternalDescriptor',
                'formtext': 'ExternalDescriptor',
                'presetIdShort': 'ExternalDescriptor',
                'childs': [
				 {
                    'AasElementType': 'File',
                    'formtext': 'descriptorName',
                    'presetIdShort': 'descriptorName',
                    'multiplcity': '1..*',
                    'description': 'File reference (local in AASX or outside) to an external descriptor description (e.g., Thing Description, GSDML, etc,).',
                    'semanticReference': 'https://admin-shell.io/idta/AssetInterfacesDescription/1/0/externalDescriptorName'
                  }
				]
			},
{
    'AasElementType': 'SubmodelElementCollection',
    'multiplcity': '0..1',
    'description': 'An abstraction of a physical or a virtual entity whose metadata and interfaces are described by a WoT Thing Description, whereas a virtual entity is the composition of one or more Things.',
    'semanticReference': 'https://admin-shell.io/idta/AssetInterfacesDescription/1/0/InteractionMetadata',
    'formtext': 'InteractionMetaData',
    'presetIdShort': 'InteractionMetaData',
    'childs': [
      {
        'AasElementType': 'SubmodelElementCollection',
        'multiplcity': '0..1',
        'description': 'All Property-based Interaction Affordances of the Thing.',
        'semanticReference': 'https://www.w3.org/2019/wot/td#hasPropertyAffordance',
        'formtext': 'properties',
        'presetIdShort': 'properties',
        'childs': [
          {
            'AasElementType': 'SubmodelElementCollection',
            'multiplcity': '0..1',
            'description': 'An Interaction Affordance that exposes state of the Thing',
            'semanticReference': 'https://admin-shell.io/idta/AssetInterfaceDescription/1/0/PropertyDefinition',
            'formtext': 'property',
            'presetIdShort': 'property{00:00}',
            'childs': [
              {
                'AasElementType': 'Property',
                'valueType' : 'xs:string',
                'formtext': 'key',
                'presetIdShort': 'key',
                'multiplcity': '0..1',
                'description': 'Optional element when the idShort of {property_name} cannot be used to reflect the desired property name due to the idShort restrictions',
                'semanticReference': 'https://admin-shell.io/idta/AssetInterfacesDescription/1/0/key'
              },
              {
                'AasElementType': 'Property',
                'valueType' : 'xs:string',				
                'formtext': 'title',
                'presetIdShort': 'title',
                'multiplcity': '0..1',
                'description': 'Provides a human-readable title (e.g., display a text for UI representation) based on a default language.',
                'semanticReference': 'https://www.w3.org/2019/wot/td#title'
              },
              {
                'AasElementType': 'Property',
                'valueType' : 'xs:boolean',				
                'formtext': 'observable',
                'presetIdShort': 'observable',
                'multiplcity': '0..1',
                'description': 'An indicator that tells that the interaction datapoint can be observed with a, e.g., subscription mechanism by an underlying protocol.',
                'semanticReference': 'https://www.w3.org/2019/wot/td#isObservable'
              },
              {
                'AasElementType': 'SubmodelElementCollection',
                'multiplcity': '1',
                'description': 'Provides a list of restricted set of values that the asset can provide as datapoint value.',
                'semanticReference': 'https://www.w3.org/2019/wot/td#hasForm',
                'formtext': 'forms',
                'presetIdShort': 'forms',
                'childs': [
                  {
                    'AasElementType': 'SubmodelElementCollection',
                    'formtext': 'HTTP Form',
                    'presetIdShort': 'form{00:00}',
                    'multiplcity': '0..1',
                    'description': 'HTTP form',
                    'semanticReference': 'https://www.w3.org/2019/wot/td#hasHTTPForm',
                    'childs': [
                      {
                        'AasElementType': 'Property',
						'valueType' : 'xs:string',
                        'formtext': 'contentType',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2019/wot/hypermedia#forContentType',
                        'presetIdShort': 'contentType',
                        'description': 'Indicates the datapoint media type specified by IANA.'
                      },
                      {
                        'AasElementType': 'Property',
						'valueType' : 'xs:string',						
                        'formtext': 'subprotocol',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2019/wot/hypermedia#forSubProtocol',
                        'presetIdShort': 'subprotocol',
                        'description': 'Indicates the exact mechanism by which an interaction will be accomplished for a given protocol when there are multiple options.'
                      },
                      {
                        'AasElementType': 'Property',
						'valueType' : 'xs:string',						
                        'formtext': 'href',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2019/wot/hypermedia#hasTarget',
                        'presetIdShort': 'href',
                        'description': 'Target IRI relative path or full IRI of assets datapoint.The relative endpoint definition in href is always relative to base defined in EndpointMetadata'
                      },
                      {
                        'AasElementType': 'SubmodelElementCollection',
                        'formtext': 'security',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2019/wot/td#hasSecurityConfiguration',
                        'presetIdShort': 'security',
                        'description': 'Selects one or more of the security scheme(s) that can be applied at runtime from the collection of security schemes defines in securityDefinitions',
                        'childs': [
                          {
                            'AasElementType': 'ReferenceElement',
                            'formtext': 'security',
                            'multiplcity': '0..*',
                            'semanticReference': 'https://www.w3.org/2019/wot/td#hasSecurityConfiguration',
                            'presetIdShort': '',
                            'description': 'Reference element to security scheme definition'
                          }
                        ]
                      },
                      {
                        'AasElementType': 'Property',
						'valueType' : 'xs:string',						
                        'formtext': 'htv_methodName',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2011/http#methodName',
                        'presetIdShort': 'htv_methodName',
                        'description': ''
                      },
                      {
                        'AasElementType': 'SubmodelElementCollection',
                        'formtext': 'htv_header',
                        'multiplcity': '0..*',
                        'semanticReference': 'https://www.w3.org/2011/http#headers',
                        'presetIdShort': '',
                        'description': ' Information for http message header definition',
                        'childs': [
                          {
                            'AasElementType': 'Property',
							'valueType' : 'xs:string',
                            'formtext': 'htv_fieldName',
                            'multiplcity': '0..1',
                            'semanticReference': 'https://www.w3.org/2011/http#fieldName',
                            'presetIdShort': 'htv_fieldName',
                            'description': ''
                          },
                          {
                            'AasElementType': 'Property',
							'valueType' : 'xs:string',							
                            'formtext': 'htv_fieldValue',
                            'multiplcity': '0..1',
                            'semanticReference': 'https://www.w3.org/2011/http#fieldValue',
                            'presetIdShort': 'htv_fieldValue',
                            'description': ''
                          }
                        ]
                      }
                    ]
                  },
                  {
                    'AasElementType': 'SubmodelElementCollection',
                    'formtext': 'MQTT Form',
                    'presetIdShort': 'form{00:00}',
                    'multiplcity': '0..1',
                    'description': 'MQTT form',
                    'semanticReference': 'https://www.w3.org/2019/wot/td#hasMQTTForm',
                    'childs': [
                      {
                        'AasElementType': 'Property',
						'valueType' : 'xs:string',
                        'formtext': 'contentType',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2019/wot/hypermedia#forContentType',
                        'presetIdShort': 'contentType',
                        'description': 'Indicates the datapoint media type specified by IANA.'
                      },
                      {
                        'AasElementType': 'Property',
						'valueType' : 'xs:string',
                        'formtext': 'subprotocol',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2019/wot/hypermedia#forSubProtocol',
                        'presetIdShort': 'subprotocol',
                        'description': 'Indicates the exact mechanism by which an interaction will be accomplished for a given protocol when there are multiple options.'
                      },
                      {
                        'AasElementType': 'Property',
						'valueType' : 'xs:string',
                        'formtext': 'href',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2019/wot/hypermedia#hasTarget',
                        'presetIdShort': 'href',
                        'description': 'Target IRI relative path or full IRI of assets datapoint.The relative endpoint definition in href is always relative to base defined in EndpointMetadata'
                      },
                      {
                        'AasElementType': 'SubmodelElementCollection',
                        'formtext': 'security',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2019/wot/td#hasSecurityConfiguration',
                        'presetIdShort': 'security',
                        'description': 'Selects one or more of the security scheme(s) that can be applied at runtime from the collection of security schemes defines in securityDefinitions',
                        'childs': [
                          {
                            'AasElementType': 'ReferenceElement',
                            'formtext': 'security',
                            'multiplcity': '0..*',
                            'semanticReference': 'https://www.w3.org/2019/wot/td#hasSecurityConfiguration',
                            'presetIdShort': '',
                            'description': 'Reference element to security scheme definition'
                          }
                        ]
                      },
                      {
                        'AasElementType': 'Property',
						'valueType' : 'xs:boolean',
                        'formtext': 'mqv_retain',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2019/wot/mqtt#hasRetainFlag',
                        'presetIdShort': 'mqv_retain',
                        'description': 'It is an indicator that tells the broker to always retain last published payload.'
                      },
                      {
                        'AasElementType': 'Property',
						'valueType' : 'xs:string',
                        'formtext': 'mqv_controlPacket',
                        'multiplcity': '1',
                        'semanticReference': 'https://www.w3.org/2019/wot/mqtt#ControlPacket',
                        'presetIdShort': 'mqv_controlPacket',
                        'description': 'Defines the method associated to the datapoint in relation to the broker.'
                      },
                      {
                        'AasElementType': 'Property',
						'valueType' : 'xs:string',
                        'formtext': 'mqv_qos',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2019/wot/mqtt#hasQoSFlag',
                        'presetIdShort': 'mqv_qos',
                        'description': 'Defined the level of guarantee for message delivery between clients.'
                      }
                    ]
                  },
                  {
                    'AasElementType': 'SubmodelElementCollection',
                    'formtext': 'MODBUS Form',
                    'presetIdShort': 'form{00:00}',
                    'multiplcity': '0..1',
                    'description': 'MODBUS form',
                    'semanticReference': 'https://www.w3.org/2019/wot/td#hasMODBUSForm',
                    'childs': [
                      {
                        'AasElementType': 'Property',
						'valueType' : 'xs:string',						
                        'formtext': 'contentType',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2019/wot/hypermedia#forContentType',
                        'presetIdShort': 'contentType',
                        'description': 'Indicates the datapoint media type specified by IANA.'
                      },
                      {
                        'AasElementType': 'Property',
						'valueType' : 'xs:string',						
                        'formtext': 'subprotocol',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2019/wot/hypermedia#forSubProtocol',
                        'presetIdShort': 'subprotocol',
                        'description': 'Indicates the exact mechanism by which an interaction will be accomplished for a given protocol when there are multiple options.'
                      },
                      {
                        'AasElementType': 'Property',
						'valueType' : 'xs:string',						
                        'formtext': 'href',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2019/wot/hypermedia#hasTarget',
                        'presetIdShort': 'href',
                        'description': 'Target IRI relative path or full IRI of assets datapoint.The relative endpoint definition in href is always relative to base defined in EndpointMetadata'
                      },
                      {
                        'AasElementType': 'SubmodelElementCollection',
                        'formtext': 'security',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2019/wot/td#hasSecurityConfiguration',
                        'presetIdShort': 'security',
                        'description': 'Selects one or more of the security scheme(s) that can be applied at runtime from the collection of security schemes defines in securityDefinitions',
                        'childs': [
                          {
                            'AasElementType': 'ReferenceElement',
                            'formtext': 'security',
                            'multiplcity': '0..*',
                            'semanticReference': 'https://www.w3.org/2019/wot/td#hasSecurityConfiguration',
                            'presetIdShort': '',
                            'description': 'Reference element to security scheme definition'
                          }
                        ]
                      },
                      {
                        'AasElementType': 'Property',
						'valueType' : 'xs:string',						
                        'formtext': 'modv_function',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2019/wot/modbus#hasFunction',
                        'presetIdShort': 'modv_function',
                        'description': 'Abstraction of the Modbus function code sent during a request. A function value can be either readCoil, readDeviceIdentification, readDiscreteInput, readHoldingRegisters, readInputRegisters, writeMultipleCoils, writeMultipleHoldingRegisters, writeSingleCoil, or  writeSingleHoldingRegister'
                      },
                      {
                        'AasElementType': 'Property',
						'valueType' : 'xs:string',						
                        'formtext': 'modv_entity',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2019/wot/modbus#hasEntity',
                        'presetIdShort': 'modv_entity',
                        'description': 'A registry type to let the runtime automatically detect the right function code. An entity value can be Coil, DiscreteInput, HoldingRegister, or InputRegister '
                      },
                      {
                        'AasElementType': 'Property',
						'valueType' : 'xs:boolean',						
                        'formtext': 'modv_zeroBasedAddressing',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2019/wot/modbus#hasZeroBasedAddressingFlag',
                        'presetIdShort': 'modv_zeroBasedAddressing',
                        'description': 'Modbus implementations can differ in the way addressing works, as the first coil/register can be either referred to as True or False.'
                      },
                      {
                        'AasElementType': 'Property',
						'valueType' : 'xs:integer',
                        'formtext': 'modv_timeout',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2019/wot/modbus#hasTimeout',
                        'presetIdShort': 'modv_timeout',
                        'description': 'Modbus response maximum waiting time. Defines how much time in milliseconds the runtime should wait until it receives a reply from the device.'
                      },
                      {
                        'AasElementType': 'Property',
						'valueType' : 'xs:integer',						
                        'formtext': 'modv_pollingTime',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2019/wot/modbus#hasPollingTime',
                        'presetIdShort': 'modv_pollingTime',
                        'description': 'Modbus TCP maximum polling rate. The Modbus specification does not define a maximum or minimum allowed polling rate, however specific implementations might introduce such limits. Defined as integer of milliseconds.'
                      },
                      {
                        'AasElementType': 'Property',
						'valueType' : 'xs:string',						
                        'formtext': 'modv_type',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2019/wot/modbus#hasPayloadDataType',
                        'presetIdShort': 'modv_type',
                        'description': 'Defines the data type of the modbus asset payload. type in terms of possible sign, base type. the modv_type offers a set a types defined in XML schema'
                      },
                      {
                        'AasElementType': 'Property',
						'valueType' : 'xs:boolean',
                        'formtext': 'modv_mostSignificantByte',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2019/wot/modbus#hasMostSignificantByte',
                        'presetIdShort': 'modv_mostSignificantByte',
                        'description': 'Define the base URI that is used for all relative URI references throughout a TD document. In TD instances, all relative URIs are resolved relative to the base URI using the algorithm defined in [RFC3986].'
                      },
                      {
                        'AasElementType': 'Property',
						'valueType' : 'xs:boolean',						
                        'formtext': 'modv_mostSignificantWord',
                        'multiplcity': '0..1',
                        'semanticReference': 'https://www.w3.org/2019/wot/modbus#hasMostSignificantWord',
                        'presetIdShort': 'modv_mostSignificantWord',
                        'description': 'When true, it describes that the word order of the data in the Modbus message is the most significant word first (i.e., no word swapping). When false, it describes the least significant word first (i.e. word swapping) '
                      }
                    ]
                  }
                ]
              },
              {
                'AasElementType': 'ReferenceElement',
                'formtext': 'valueSemantics',
                'multiplcity': '0..1',
                'semanticReference': 'https://admin-shell.io/idta/AssetInterfacesDescription/1/0/valueSemantics',
                'presetIdShort': 'valueSemantics',
                'description': 'Provides additional semantic information of the value that is read/subscribed at runtime.'
              },
              {
                'AasElementType': 'Property',
                'valueType' : 'xs:string',				
                'formtext': 'type',
                'presetIdShort': 'type',
                'multiplcity': '0..1',
                'description': 'Assignment of JSON-based data types compatible with JSON Schema (one of boolean, integer, number, string, object, array, or null).',
                'semanticReference': 'https://www.w3.org/1999/02/22-rdf-syntax-ns#type'
              },
              {
                'AasElementType': 'Property',
                'valueType' : 'xs:string',
                'formtext': 'const',
                'presetIdShort': 'const',
                'multiplcity': '0..1',
                'description': 'Provides a constant value.',
                'semanticReference': 'https://www.w3.org/2019/wot/json-schema#const'
              },
              {
                'AasElementType': 'SubmodelElementCollection',
                'multiplcity': '0..1',
                'description': 'Provides a list of restricted set of values that the asset can provide as datapoint value.',
                'semanticReference': 'https://www.w3.org/2019/wot/json-schema#enum',
                'formtext': 'enum',
                'presetIdShort': 'enum',
                'childs': [
                  {
                    'AasElementType': 'Property',
					'valueType' : 'xs:string',					
                    'formtext': 'enum',
                    'presetIdShort': 'enum{00:00}',
                    'multiplcity': '0..*',
                    'description': 'Data Point Value',
                    'semanticReference': 'https://www.w3.org/2019/wot/json-schema#enum'
                  }
                ]
              },
              {
                'AasElementType': 'Property',
				'valueType' : 'xs:boolean',
                'formtext': 'default',
                'multiplcity': '0..1',
                'semanticReference': 'https://www.w3.org/2019/wot/json-schema#default',
                'presetIdShort': 'default',
                'description': 'Supply a default value. The value SHOULD validate against the data schema in which it resides.'
              },
              {
                'AasElementType': 'Property',
                'valueType' : 'xs:string',
                'formtext': 'unit',
                'multiplcity': '0..1',
                'semanticReference': 'https://schema.org/unitCode',
                'presetIdShort': 'unit',
                'description': 'Provides unit information that is used, e.g., in international science, engineering, and business. To preserve uniqueness, it is recommended that the value of the unit points to a semantic definition'
              },
              {
                'AasElementType': 'Range',
                'valueType' : 'xs:integer',				
                'formtext': 'min_max',
                'multiplcity': '0..1',
                'semanticReference': 'https://admin-shell.io/idta/AssetInterfacesDescription/1/0/minMaxRange',
                'presetIdShort': 'min_max',
                'description': 'Specifies a minimum and/or maximum numeric value for the datapoint.'
              },
              {
                'AasElementType': 'Range',
                'valueType' : 'xs:unsignedInt',
                'formtext': 'lengthRange',
                'multiplcity': '0..1',
                'semanticReference': 'https://admin-shell.io/idta/AssetInterfacesDescription/1/0/lengthRange',
                'presetIdShort': 'lengthRange',
                'description': 'Specifies the minimum and maximum length of a string.'
              },
              {
                'AasElementType': 'SubmodelElementCollection',
                'multiplcity': '1',
                'description': 'Used to define the data schema characteristics of an array payload.',
                'semanticReference': 'https://www.w3.org/2019/wot/json-schema#items',
                'formtext': 'items',
                'presetIdShort': 'items',
                'childs': [
                  {
                    'AasElementType': 'Property',
                    'valueType' : 'xs:string',
                    'formtext': 'type',
                    'presetIdShort': 'type',
                    'multiplcity': '0..1',
                    'description': 'Assignment of JSON-based data types compatible with JSON Schema (one of boolean, integer, number, string, object, array, or null).',
                    'semanticReference': 'https://www.w3.org/1999/02/22-rdf-syntax-ns#type'
                  },
                  {
                    'AasElementType': 'Property',
                    'valueType' : 'xs:string',
                    'formtext': 'const',
                    'presetIdShort': 'const',
                    'multiplcity': '0..1',
                    'description': 'Provides a constant value.',
                    'semanticReference': 'https://www.w3.org/2019/wot/json-schema#const'
                  },
                  {
                    'AasElementType': 'SubmodelElementCollection',
                    'multiplcity': '0..1',
                    'description': 'Provides a list of restricted set of values that the asset can provide as datapoint value.',
                    'semanticReference': 'https://www.w3.org/2019/wot/json-schema#enum',
                    'formtext': 'enum',
                    'presetIdShort': 'enum',
                    'childs': [
                      {
                        'AasElementType': 'Property',
                        'valueType' : 'xs:string',
                        'formtext': 'enum{00:00}',
                        'presetIdShort': 'enum{00:00}',
                        'multiplcity': '0..*',
                        'description': 'Data Point Value',
                        'semanticReference': 'https://www.w3.org/2019/wot/json-schema#enum'
                      }
                    ]
                  },
                  {
                    'AasElementType': 'Property',
                    'valueType' : 'xs:boolean',
                    'formtext': 'default',
                    'multiplcity': '0..1',
                    'semanticReference': 'https://www.w3.org/2019/wot/json-schema#default',
                    'presetIdShort': 'default',
                    'description': 'Supply a default value. The value SHOULD validate against the data schema in which it resides.'
                  },
                  {
                    'AasElementType': 'Property',
                    'valueType' : 'xs:string',
                    'formtext': 'unit',
                    'multiplcity': '0..1',
                    'semanticReference': 'https://schema.org/unitCode',
                    'presetIdShort': 'unit',
                    'description': 'Provides unit information that is used, e.g., in international science, engineering, and business. To preserve uniqueness, it is recommended that the value of the unit points to a semantic definition'
                  },
                  {
                    'AasElementType': 'Range',
                    'valueType' : 'xs:integer',
                    'formtext': 'min_max',
                    'multiplcity': '0..1',
                    'semanticReference': 'https://admin-shell.io/idta/AssetInterfacesDescription/1/0/minMaxRange',
                    'presetIdShort': 'min_max',
                    'description': 'Specifies a minimum and/or maximum numeric value for the datapoint.'
                  },
                  {
                    'AasElementType': 'Range',
                    'valueType' : 'xs:unsignedInt',
                    'formtext': 'lengthRange',
                    'multiplcity': '0..1',
                    'semanticReference': 'https://admin-shell.io/idta/AssetInterfacesDescription/1/0/lengthRange',
                    'presetIdShort': 'lengthRange',
                    'description': 'Specifies the minimum and maximum length of a string.'
                  }
                ]
              },
              {
                'AasElementType': 'Range',
                'valueType' : 'xs:unsignedInt',				
                'formtext': 'itemsRange',
                'multiplcity': '0..1',
                'semanticReference': 'https://admin-shell.io/idta/AssetInterfacesDescription/1/0/itemsRange',
                'presetIdShort': 'itemsRange',
                'description': 'Defines the minimum and maximum number of items that have to be in an array payload.'
              },
			  {
        'AasElementType': 'SubmodelElementCollection',
        'multiplcity': '0..1',
        'description': 'Nested definitions of a datapoint. Only applicable if type=object',
        'semanticReference': 'https://www.w3.org/2019/wot/json-schema#properties',
        'formtext': 'properties',
        'presetIdShort': 'properties',
        'childs': [
          {
            'AasElementType': 'SubmodelElementCollection',
            'multiplcity': '0..1',
            'description': 'An Interaction Affordance that exposes state of the Thing',
            'semanticReference': 'https://www.w3.org/2019/wot/json-schema#propertyName',
            'formtext': 'property',
            'presetIdShort': 'property{00:00}',
            'childs': [
              {
                'AasElementType': 'Property',
				'valueType' : 'xs:string',
                'formtext': 'key',
                'presetIdShort': 'key',
                'multiplcity': '0..1',
                'description': 'Optional element when the idShort of {property_name} cannot be used to reflect the desired property name due to the idShort restrictions',
                'semanticReference': 'https://admin-shell.io/idta/AssetInterfacesDescription/1/0/key'
              },
              {
                'AasElementType': 'Property',
				'valueType' : 'xs:string',
                'formtext': 'title',
                'presetIdShort': 'title',
                'multiplcity': '0..1',
                'description': 'Provides a human-readable title (e.g., display a text for UI representation) based on a default language.',
                'semanticReference': 'https://www.w3.org/2019/wot/td#title'
              },
              {
                'AasElementType': 'Property',
				'valueType' : 'xs:boolean',				
                'formtext': 'observable',
                'presetIdShort': 'observable',
                'multiplcity': '0..1',
                'description': 'An indicator that tells that the interaction datapoint can be observed with a, e.g., subscription mechanism by an underlying protocol.',
                'semanticReference': 'https://www.w3.org/2019/wot/td#isObservable'
              },
              {
                'AasElementType': 'ReferenceElement',
                'formtext': 'valueSemantics',
                'multiplcity': '0..1',
                'semanticReference': 'https://admin-shell.io/idta/AssetInterfacesDescription/1/0/valueSemantics',
                'presetIdShort': 'valueSemantics',
                'description': 'Provides additional semantic information of the value that is read/subscribed at runtime.'
              },
              {
                'AasElementType': 'Property',
				'valueType' : 'xs:string',				
                'formtext': 'type',
                'presetIdShort': 'type',
                'multiplcity': '0..1',
                'description': 'Assignment of JSON-based data types compatible with JSON Schema (one of boolean, integer, number, string, object, array, or null).',
                'semanticReference': 'https://www.w3.org/1999/02/22-rdf-syntax-ns#type'
              },
              {
                'AasElementType': 'Property',
				'valueType' : 'xs:string',				
                'formtext': 'const',
                'presetIdShort': 'const',
                'multiplcity': '0..1',
                'description': 'Provides a constant value.',
                'semanticReference': 'https://www.w3.org/2019/wot/json-schema#const'
              },
              {
                'AasElementType': 'SubmodelElementCollection',
                'multiplcity': '0..1',
                'description': 'Provides a list of restricted set of values that the asset can provide as datapoint value.',
                'semanticReference': 'https://www.w3.org/2019/wot/json-schema#enum',
                'formtext': 'enum',
                'presetIdShort': 'enum',
                'childs': [
                  {
                    'AasElementType': 'Property',
					'valueType' : 'xs:string',					
                    'formtext': 'enum',
                    'presetIdShort': 'enum{00:00}',
                    'multiplcity': '0..*',
                    'description': 'Data Point Value',
                    'semanticReference': 'https://www.w3.org/2019/wot/json-schema#enum'
                  }
                ]
              },
              {
                'AasElementType': 'Property',
				'valueType' : 'xs:boolean',
                'formtext': 'default',
                'multiplcity': '0..1',
                'semanticReference': 'https://www.w3.org/2019/wot/json-schema#default',
                'presetIdShort': 'default',
                'description': 'Supply a default value. The value SHOULD validate against the data schema in which it resides.'
              },
              {
                'AasElementType': 'Property',
				'valueType' : 'xs:string',				
                'formtext': 'unit',
                'multiplcity': '0..1',
                'semanticReference': 'https://schema.org/unitCode',
                'presetIdShort': 'unit',
                'description': 'Provides unit information that is used, e.g., in international science, engineering, and business. To preserve uniqueness, it is recommended that the value of the unit points to a semantic definition'
              },
              {
                'AasElementType': 'Range',
				'valueType' : 'xs:integer',				
                'formtext': 'min_max',
                'multiplcity': '0..1',
                'semanticReference': 'https://admin-shell.io/idta/AssetInterfacesDescription/1/0/minMaxRange',
                'presetIdShort': 'min_max',
                'description': 'Specifies a minimum and/or maximum numeric value for the datapoint.'
              },
              {
                'AasElementType': 'Range',
				'valueType' : 'xs:unsignedInt',				
                'formtext': 'lengthRange',
                'multiplcity': '0..1',
                'semanticReference': 'https://admin-shell.io/idta/AssetInterfacesDescription/1/0/lengthRange',
                'presetIdShort': 'lengthRange',
                'description': 'Specifies the minimum and maximum length of a string.'
              },
              {
                'AasElementType': 'SubmodelElementCollection',
                'multiplcity': '0..1',
                'description': 'Used to define the data schema characteristics of an array payload.',
                'semanticReference': 'https://www.w3.org/2019/wot/json-schema#items',
                'formtext': 'items',
                'presetIdShort': 'items',
                'childs': [
                  {
                    'AasElementType': 'Property',
					'valueType' : 'xs:string',
                    'formtext': 'type',
                    'presetIdShort': 'type',
                    'multiplcity': '0..1',
                    'description': 'Assignment of JSON-based data types compatible with JSON Schema (one of boolean, integer, number, string, object, array, or null).',
                    'semanticReference': 'https://www.w3.org/1999/02/22-rdf-syntax-ns#type'
                  },
                  {
                    'AasElementType': 'Property',
					'valueType' : 'xs:string',					
                    'formtext': 'const',
                    'presetIdShort': 'const',
                    'multiplcity': '0..1',
                    'description': 'Provides a constant value.',
                    'semanticReference': 'https://www.w3.org/2019/wot/json-schema#const'
                  },
                  {
                    'AasElementType': 'SubmodelElementCollection',
                    'multiplcity': '0..1',
                    'description': 'Provides a list of restricted set of values that the asset can provide as datapoint value.',
                    'semanticReference': 'https://www.w3.org/2019/wot/json-schema#enum',
                    'formtext': 'enum',
                    'presetIdShort': 'enum',
                    'childs': [
                      {
                        'AasElementType': 'Property',
						'valueType' : 'xs:string',						
                        'formtext': 'enum{00:00}',
                        'presetIdShort': 'enum{00:00}',
                        'multiplcity': '0..*',
                        'description': 'Data Point Value',
                        'semanticReference': 'https://www.w3.org/2019/wot/json-schema#enum'
                      }
                    ]
                  },
                  {
                    'AasElementType': 'Property',
					'valueType' : 'xs:boolean',					
                    'formtext': 'default',
                    'multiplcity': '0..1',
                    'semanticReference': 'https://www.w3.org/2019/wot/json-schema#default',
                    'presetIdShort': 'default',
                    'description': 'Supply a default value. The value SHOULD validate against the data schema in which it resides.'
                  },
                  {
                    'AasElementType': 'Property',
					'valueType' : 'xs:string',					
                    'formtext': 'unit',
                    'multiplcity': '0..1',
                    'semanticReference': 'https://schema.org/unitCode',
                    'presetIdShort': 'unit',
                    'description': 'Provides unit information that is used, e.g., in international science, engineering, and business. To preserve uniqueness, it is recommended that the value of the unit points to a semantic definition'
                  },
                  {
                    'AasElementType': 'Range',
					'valueType' : 'xs:integer',					
                    'formtext': 'min_max',
                    'multiplcity': '0..1',
                    'semanticReference': 'https://admin-shell.io/idta/AssetInterfacesDescription/1/0/minMaxRange',
                    'presetIdShort': 'min_max',
                    'description': 'Specifies a minimum and/or maximum numeric value for the datapoint.'
                  },
                  {
                    'AasElementType': 'Range',
					'valueType' : 'xs:unsignedInt',					
                    'formtext': 'lengthRange',
                    'multiplcity': '0..1',
                    'semanticReference': 'https://admin-shell.io/idta/AssetInterfacesDescription/1/0/lengthRange',
                    'presetIdShort': 'lengthRange',
                    'description': 'Specifies the minimum and maximum length of a string.'
                  }
                ]
              },
              {
                'AasElementType': 'Range',
				'valueType' : 'xs:unsignedInt',				
                'formtext': 'itemsRange',
                'multiplcity': '0..1',
                'semanticReference': 'https://admin-shell.io/idta/AssetInterfacesDescription/1/0/itemsRange',
                'presetIdShort': 'itemsRange',
                'description': 'Defines the minimum and maximum number of items that have to be in an array payload.'
              }
            ]
          }
        ]
      }
            ]
          }
        ]
      }
    ]
  }
		   ]
        }
	}");
        
    }
}
