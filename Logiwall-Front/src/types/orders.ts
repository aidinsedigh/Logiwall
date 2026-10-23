export interface IorderResponse {
  totalCount: number;
  result: [
    {
      id: number;
      channelId: number;
      status: number;
      statusName: string;
      senderName: string;
      senderLastName: string;
      senderPhoneNumber: string;
      originCity: number;
      originCityName: string;
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
      destinationCityName: string;
      destinationLat: string;
      destinationLng: string;
      destinationAddress: string;
      destinationPostalCode: string;
      destinationPlaque: string;
      destinationUnit: string;
      description: null;
      serviceType: number;
      serviceProviderId: number;
      serviceSLA: number;
      serviceProviderTrackingCode: null;
      packagingAmount: number;
      serviceAmount: number;
      insuranceAmount: number;
      tax: number;
      finalAmount: number;
      pickupServiceId: number;
      creationDate: string;
      modificationDate: null;
      pickupDateTime: null;
      customerId: number;
      orderInqueryOptions: [] | null;
      orderParcels: [] | null;
      orderPayments: [] | null;
      orderStatuses: [] | null;
    },
  ];
}
