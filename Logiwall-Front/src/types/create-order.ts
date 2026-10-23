export interface IcreateOrder {
  channelId: number;
  senderName: string;
  senderLastName: string;
  senderPhoneNumber: string;
  originCity: number;
  originLat: string;
  originLng: string;
  originAddress: string;
  originPostalCode: string;
  originalPlaque: string;
  originalUnit: string;
  receiverName: string;
  receiverLastName: string;
  receiverPhoneNumber: string;
  destinationCountry: number;
  destinationCity: number;
  destinationLat: string;
  destinationLng: string;
  destinationAddress: string;
  destinationPostalCode: string;
  destinationPlaque: string;
  destinationUnit: string;
  description: string;
  orderParcels: [
    {
      parcelTypeId: number;
      packageTypeId: number;
      parcelValue: number;
      length: number;
      width: number;
      height: number;
      weight: number;
      cod: number;
    },
  ];
}

export interface IcreateOrderResponse {
  orderId: number;
  orderNumber: string;
}
// -----------------------------------------------------------------------

export interface IgoPayPayload {
  orderId: number;
  pickupOptionId: number;
  paymentType: number;
  channel: string;
  callbackUrl: string;
}

export interface IgoPayResponse {
  paymentLink: string;
  referenceCode: string;
  requestId: number;
}

// ------------------------------------------------------------------------

export interface IselectOption {
  destinationAddress: string;
  destinationCity: null | string | number;
  destinationPlaque: string;
  destinationUnit: string;
  option: string | null;
  originAddress: string;
  originCity: string | null;
  originPlaque: string;
  originUnit: string;
  parcels: [
    {
      id: string;
      parcelType: string;
      packageJetId: string;
      packageJetDescription: string;
      title: null;
      weight: string;
      length: string;
      width: string;
      height: string;
      value: string;
      count: string;
      needPacking: string;
    },
  ];
  pickupOptions: [
    {
      id: number;
      providerPickupOptionId: null | string;
      finalAmount: number;
      basePrice: number;
      itemPrice: number;
      tax: number;
      description: string;
    },
  ];
  receiverLastName: string;
  receiverName: string;
  receiverPhoneNumber: string;
  senderLastName: string;
  senderName: string;
  senderPhoneNumber: string;
}

// -------------------------------------------------------------------------

export interface IgetOptions {
  providerId: number;
  providerName: string;
  orderOptions: [
    {
      id: number;
      delivereDate: string;
      baseAmount: number;
      packageAmount: number;
      taxAmount: number;
      totalAmount: number;
    },
  ];
  pickupDates: [
    {
      id: string;
      description: string;
      times: [
        {
          id: string;
          description: string;
        },
      ];
    },
  ];
}
