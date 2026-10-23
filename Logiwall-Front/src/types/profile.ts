export interface IchangeProfileInfo {
  firstName: string;
  lastName: string;
  nationalCode: string;
  email: string;
  gender: number;
}

export interface IchangeProfileInfoResponse {
  data: {
    userId: number;
    username: string;
    firstName: string;
    lastName: string;
    phoneNumber: string;
    email: string;
    kind: number;
    gender: number;
    birthDate: null | string;
    nationalCode: string;
  };
  succuss: boolean;
  message: string;
}

// -------------------------------------------------------------

export interface IchangePasswordResponse {
  data: boolean;
  message: string;
  succuss: boolean;
}
