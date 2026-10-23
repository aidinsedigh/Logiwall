import { Controller } from 'react-hook-form';
import OtpInput from 'react-otp-input';
import { FormHelperText, Stack } from '@mui/material';
import { FC, useState } from 'react';

interface IotpInput {
  control: any;
  name: string;
  onComplete?: (otp: string) => void;
}

const OtpInputComponent: FC<IotpInput> = ({ control, onComplete, name }) => {
  const [focusedIndex, setFocusedIndex] = useState<number | null>(null); // Track the focused input index

  return (
    <Controller
      name={name}
      control={control}
      render={({ field: { onChange, value }, fieldState: { error } }) => (
        <Stack>
          <Stack direction="row" justifyContent="center">
            <OtpInput
              value={value}
              onChange={(otp) => {
                onChange(otp);
                if (otp.length === 5 && onComplete) {
                  onComplete(otp); // Trigger onComplete when OTP is fully entered
                }
              }}
              numInputs={5}
              shouldAutoFocus
              containerStyle={{
                direction: 'ltr',
                display: 'flex',
                gap: '8px',
              }}
              renderInput={(props, index) => {
                const isFocused = focusedIndex === index;
                return (
                  <input
                    {...props}
                    placeholder="-"
                    inputMode="tel"
                    onFocus={() => setFocusedIndex(index)} // Set focus index when input is focused
                    onBlur={() => setFocusedIndex(null)} // Reset focus index on blur
                    style={{
                      direction: 'ltr',
                      textAlign: 'center',
                      fontSize: '12px',
                      width: '40px',
                      height: '40px',
                      borderColor: isFocused ? '#4caf50' : '#ccc', // Focus border color
                      borderWidth: '1px', // Focus border width
                      outline: 'none', // Remove default outline
                    }}
                  />
                );
              }}
            />
          </Stack>
          {error && (
            <FormHelperText sx={{ px: 2,textAlign:'center',mt:2 }} error>
              {error.message}
            </FormHelperText>
          )}
        </Stack>
      )}
    />
  );
};

export default OtpInputComponent;
